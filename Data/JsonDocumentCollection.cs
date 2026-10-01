using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EventEase.Data
{
    /// <summary>
    /// Local document store implementation of INoSqlCollection.
    /// Stores NoSQL documents as JSON collections on disk for zero-dependency local execution
    /// when an external MongoDB service is not actively running.
    /// </summary>
    public class JsonDocumentCollection<T> : INoSqlCollection<T>, IDisposable
    {
        private readonly string _filePath;
        private readonly List<T> _data = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private System.Threading.Timer? _saveTimer;
        private bool _pendingWrite = false;
        private const int SaveDebounceMs = 500; // Batch writes within 500ms
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        private bool _disposed = false;

        public JsonDocumentCollection(string dataDirectory, string collectionName)
        {
            Directory.CreateDirectory(dataDirectory);
            _filePath = Path.Combine(dataDirectory, $"{collectionName}.json");
            LoadFromDisk();
        }

        private void LoadFromDisk()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var items = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
                        if (items != null)
                        {
                            _data.AddRange(items);
                        }
                    }
                }
                catch
                {
                    // If file is unreadable, start fresh
                }
            }
        }

        private void SaveToDisk()
        {
            // Mark that we have pending writes, then schedule/reschedule the actual write
            _pendingWrite = true;

            // Reuse one timer; every mutation moves the write deadline forward.
            _saveTimer ??= new System.Threading.Timer(
                async state => await FlushPendingWriteAsync(),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
            _saveTimer.Change(SaveDebounceMs, Timeout.Infinite);
        }

        private async Task FlushPendingWriteAsync()
        {
            try
            {
                await _lock.WaitAsync();
                try
                {
                    if (!_pendingWrite || _disposed) return;

                    // Serialize and write while holding the collection lock so a timer
                    // cannot enumerate the list while a request is mutating it.
                    var json = JsonSerializer.Serialize(_data, JsonOptions);
                    WriteSnapshot(json);
                    _pendingWrite = false;
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch
            {
                // Preserve the pending flag so disposal or a later write can retry.
            }
        }

        private void WriteSnapshot(string json)
        {
            var temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _filePath, true);
        }

        private static Func<T, bool> GetCompiledFilter(Expression<Func<T, bool>> filter)
        {
            // Expressions may capture request-specific values. Caching by ToString()
            // reuses a delegate bound to an earlier closure and can return wrong records.
            return filter.Compile();
        }

        private static T CloneDocument(T document)
        {
            var snapshot = JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);
            return JsonSerializer.Deserialize<T>(snapshot, JsonOptions)!;
        }

        public async Task<List<T>> FindAllAsync()
        {
            List<T> documents;
            await _lock.WaitAsync();
            try
            {
                documents = _data.ToList();
            }
            finally
            {
                _lock.Release();
            }

            return documents.Select(CloneDocument).ToList();
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter)
        {
            List<T> documents;
            await _lock.WaitAsync();
            try
            {
                var compiled = GetCompiledFilter(filter);
                documents = _data.Where(compiled).ToList();
            }
            finally
            {
                _lock.Release();
            }

            return documents.Select(CloneDocument).ToList();
        }

        public async Task<T?> FindOneAsync(Expression<Func<T, bool>> filter)
        {
            T? document;
            await _lock.WaitAsync();
            try
            {
                var compiled = GetCompiledFilter(filter);
                document = _data.FirstOrDefault(compiled);
            }
            finally
            {
                _lock.Release();
            }

            return document is null ? default : CloneDocument(document);
        }

        public async Task<int> GetMaxIdAsync(Expression<Func<T, int>> idSelector)
        {
            await _lock.WaitAsync();
            try
            {
                if (!_data.Any()) return 0;
                var compiled = idSelector.Compile();
                return _data.Max(compiled);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task InsertOneAsync(T document)
        {
            var snapshot = CloneDocument(document);
            await _lock.WaitAsync();
            try
            {
                _data.Add(snapshot);
                SaveToDisk();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task InsertManyAsync(IEnumerable<T> documents)
        {
            if (documents == null) return;
            var snapshots = documents.Select(CloneDocument).ToList();
            if (snapshots.Count == 0) return;

            await _lock.WaitAsync();
            try
            {
                _data.AddRange(snapshots);
                SaveToDisk();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ReplaceOneAsync(Expression<Func<T, bool>> filter, T document)
        {
            var snapshot = CloneDocument(document);
            await _lock.WaitAsync();
            try
            {
                var compiled = GetCompiledFilter(filter);
                var index = _data.FindIndex(new Predicate<T>(compiled));
                if (index >= 0)
                {
                    _data[index] = snapshot;
                }
                else
                {
                    _data.Add(snapshot);
                }
                SaveToDisk();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task DeleteOneAsync(Expression<Func<T, bool>> filter)
        {
            await _lock.WaitAsync();
            try
            {
                var compiled = GetCompiledFilter(filter);
                var index = _data.FindIndex(new Predicate<T>(compiled));
                if (index >= 0)
                {
                    _data.RemoveAt(index);
                    SaveToDisk();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task DeleteManyAsync(Expression<Func<T, bool>> filter)
        {
            await _lock.WaitAsync();
            try
            {
                var compiled = GetCompiledFilter(filter);
                _data.RemoveAll(new Predicate<T>(compiled));
                SaveToDisk();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<long> CountDocumentsAsync(Expression<Func<T, bool>> filter)
        {
            await _lock.WaitAsync();
            try
            {
                var compiled = GetCompiledFilter(filter);
                return _data.Count(compiled);
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _saveTimer?.Dispose();
            _saveTimer = null;

            _lock.Wait();
            try
            {
                if (_pendingWrite)
                {
                    var json = JsonSerializer.Serialize(_data, JsonOptions);
                    WriteSnapshot(json);
                    _pendingWrite = false;
                }
                _disposed = true;
            }
            catch
            {
                // Ignore transient write issues during shutdown.
                _disposed = true;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
