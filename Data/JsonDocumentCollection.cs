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

            // Dispose existing timer if any
            _saveTimer?.Dispose();

            // Schedule a debounced write to disk
            _saveTimer = new System.Threading.Timer(
                state =>
                {
                    try
                    {
                        if (_pendingWrite && !_disposed)
                        {
                            var json = JsonSerializer.Serialize(_data, JsonOptions);
                            File.WriteAllText(_filePath, json);
                            _pendingWrite = false;
                        }
                    }
                    catch
                    {
                        // Ignore transient write issues
                    }
                },
                null,
                SaveDebounceMs,
                Timeout.Infinite); // Execute once, then stop
        }

        public async Task<List<T>> FindAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                return _data.ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter)
        {
            await _lock.WaitAsync();
            try
            {
                var compiled = filter.Compile();
                return _data.Where(compiled).ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<T?> FindOneAsync(Expression<Func<T, bool>> filter)
        {
            await _lock.WaitAsync();
            try
            {
                var compiled = filter.Compile();
                return _data.FirstOrDefault(compiled);
            }
            finally
            {
                _lock.Release();
            }
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
            await _lock.WaitAsync();
            try
            {
                _data.Add(document);
                SaveToDisk();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task InsertManyAsync(IEnumerable<T> documents)
        {
            if (documents == null || !documents.Any()) return;

            await _lock.WaitAsync();
            try
            {
                _data.AddRange(documents);
                SaveToDisk();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ReplaceOneAsync(Expression<Func<T, bool>> filter, T document)
        {
            await _lock.WaitAsync();
            try
            {
                var compiled = filter.Compile();
                var index = _data.FindIndex(new Predicate<T>(compiled));
                if (index >= 0)
                {
                    _data[index] = document;
                }
                else
                {
                    _data.Add(document);
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
                var compiled = filter.Compile();
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
                var compiled = filter.Compile();
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
                var compiled = filter.Compile();
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

            // Cancel pending timer
            _saveTimer?.Dispose();
            _saveTimer = null;

            // Flush any pending writes before disposal
            if (_pendingWrite)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_data, JsonOptions);
                    File.WriteAllText(_filePath, json);
                    _pendingWrite = false;
                }
                catch
                {
                    // Ignore transient write issues
                }
            }

            _lock?.Dispose();
            _disposed = true;
        }
    }
}
