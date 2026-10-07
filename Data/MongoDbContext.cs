using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventEase.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EventEase.Data
{
    /// <summary>
    /// NoSQL Database Context for EventEase.
    /// Manages MongoDB collections (Events, RSVPs, Attendances, Users).
    /// Connects to MongoDB when available or gracefully falls back to local NoSQL JSON document store
    /// if an external MongoDB daemon is not running on the host machine.
    /// </summary>
    public class MongoDbContext : IDisposable
    {
        public INoSqlCollection<Event> Events { get; private set; } = null!;
        public INoSqlCollection<RSVP> RSVPs { get; private set; } = null!;
        public INoSqlCollection<Attendance> Attendances { get; private set; } = null!;
        public INoSqlCollection<ApplicationUser> Users { get; private set; } = null!;

        private IMongoDatabase? _mongoDatabase;
        private readonly bool _isMongoMode;
        private readonly SemaphoreSlim _jsonCounterLock = new(1, 1);
        private readonly Dictionary<string, int> _jsonCounters = new();

        public bool IsConnectedToLiveMongo { get; private set; }
        public string ConnectionInfo { get; private set; } = string.Empty;

        public MongoDbContext(IConfiguration configuration, ILogger<MongoDbContext> logger)
        {
            var connectionString = configuration.GetConnectionString("MongoConnection")
                ?? configuration["MongoDb:ConnectionString"]
                ?? "mongodb://localhost:27017";
            var databaseName = configuration["MongoDb:DatabaseName"] ?? "EventEaseDb";

            bool connected = false;
            try
            {
                var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
                clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(1500);
                clientSettings.ConnectTimeout = TimeSpan.FromMilliseconds(1500);

                var client = new MongoClient(clientSettings);
                // Quick ping to check if MongoDB is active with proper timeout handling
                var pingTask = client.GetDatabase("admin").RunCommandAsync((Command<BsonDocument>)"{ping:1}");

                // Use Task.WaitAny to avoid thread pool starvation from blocking wait
                var completedIndex = Task.WaitAny(new[] { pingTask }, 1500);

                if (completedIndex >= 0 && !pingTask.IsFaulted && !pingTask.IsCanceled)
                {
                    var database = client.GetDatabase(databaseName);
                    _mongoDatabase = database;
                    _isMongoMode = true;
                    Events = new MongoCollectionWrapper<Event>(database.GetCollection<Event>("events"));
                    RSVPs = new MongoCollectionWrapper<RSVP>(database.GetCollection<RSVP>("rsvps"));
                    Attendances = new MongoCollectionWrapper<Attendance>(database.GetCollection<Attendance>("attendances"));
                    Users = new MongoCollectionWrapper<ApplicationUser>(database.GetCollection<ApplicationUser>("users"));

                    // Setup Unique Index on RSVP (EventId, Email) in MongoDB
                    // This prevents duplicate RSVPs at the database level
                    try
                    {
                        var rsvpCollection = database.GetCollection<RSVP>("rsvps");
                        var indexKeys = Builders<RSVP>.IndexKeys.Ascending(r => r.EventId).Ascending(r => r.Email);
                        var indexOptions = new CreateIndexOptions { Unique = true, Name = "IX_RSVP_EventId_Email" };
                        rsvpCollection.Indexes.CreateOne(new CreateIndexModel<RSVP>(indexKeys, indexOptions));
                    }
                    catch
                    {
                        // Index already created or ignored
                    }

                    // Recommended additional indexes for production performance:
                    // - Events: OrganizerId (for GetAllByOrganizerAsync)
                    // - Events: Date (for sorting/filtering)
                    // - Attendances: RSVPId (for GetByRsvpIdAsync)
                    // - Attendances: EventId (for GetCheckedInCountByEventIdAsync)
                    // - Users: NormalizedEmail (for FindByEmailAsync)
                    // - Users: NormalizedUserName (for FindByNameAsync)
                    // These are not auto-created to allow flexible deployment configurations.

                    connected = true;
                    IsConnectedToLiveMongo = true;
                    ConnectionInfo = $"Live MongoDB ({connectionString}/{databaseName})";
                    logger.LogInformation(">>> [NoSQL Database] Connected to live MongoDB server at {ConnectionString}/{DatabaseName}", connectionString, databaseName);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(">>> [NoSQL Database] Live MongoDB not detected at {ConnectionString} ({Message}).", connectionString, ex.Message);
            }

            if (!connected)
            {
                var dataDir = Path.Combine(AppContext.BaseDirectory, "App_Data", "NoSqlDb");
                Events = new JsonDocumentCollection<Event>(dataDir, "events");
                RSVPs = new JsonDocumentCollection<RSVP>(dataDir, "rsvps");
                Attendances = new JsonDocumentCollection<Attendance>(dataDir, "attendances");
                Users = new JsonDocumentCollection<ApplicationUser>(dataDir, "users");
                IsConnectedToLiveMongo = false;
                ConnectionInfo = $"Local NoSQL Document Store ({dataDir})";
                logger.LogInformation(">>> [NoSQL Database] Operating in Local NoSQL Document Store mode at {DataDir}", dataDir);
            }
        }

        public async Task<int> GetNextEventIdAsync()
        {
            return await GetNextIdAsync("EventId");
        }

        public async Task<int> GetNextRsvpIdAsync()
        {
            return await GetNextIdAsync("RsvpId");
        }

        public async Task<int> GetNextAttendanceIdAsync()
        {
            return await GetNextIdAsync("AttendanceId");
        }

        public async Task<int> GetNextCustomFieldIdAsync()
        {
            return await GetNextIdAsync("CustomFieldId");
        }

        /// <summary>
        /// Raises the MongoDB counter for the given name to at least <paramref name="minimumValue"/>.
        /// Uses $max with upsert so the counter is only ever raised, never lowered. Idempotent.
        /// No-op in local JSON mode (JSON counters are initialized from existing data automatically).
        /// </summary>
        public async Task EnsureCounterAtLeastAsync(string counterName, int minimumValue)
        {
            if (!_isMongoMode || _mongoDatabase == null || minimumValue <= 0)
            {
                return;
            }

            var counters = _mongoDatabase.GetCollection<Counter>("counters");
            await counters.UpdateOneAsync(
                Builders<Counter>.Filter.Eq(c => c.Id, counterName),
                Builders<Counter>.Update.Max(c => c.Value, minimumValue),
                new UpdateOptions { IsUpsert = true });
        }

        /// <summary>
        /// Startup self-heal: synchronizes the MongoDB ID counters with the maximum IDs
        /// already stored in the collections. This repairs databases where documents were
        /// seeded or imported with hardcoded IDs while the counters were left uninitialized,
        /// which otherwise causes E11000 duplicate key errors on the next inserts.
        /// </summary>
        public async Task SynchronizeCountersAsync()
        {
            if (!_isMongoMode || _mongoDatabase == null)
            {
                return;
            }

            await EnsureCounterAtLeastAsync("EventId", await Events.GetMaxIdAsync(e => e.Id));
            await EnsureCounterAtLeastAsync("RsvpId", await RSVPs.GetMaxIdAsync(r => r.Id));
            await EnsureCounterAtLeastAsync("AttendanceId", await Attendances.GetMaxIdAsync(a => a.Id));
            await EnsureCounterAtLeastAsync("CustomFieldId", await GetMaxCustomFieldIdAsync());
        }

        private async Task<int> GetNextIdAsync(string counterName)
        {
            if (_isMongoMode && _mongoDatabase != null)
            {
                // Atomic findAndModify with $inc guarantees unique IDs under concurrency
                var counters = _mongoDatabase.GetCollection<Counter>("counters");
                var filter = Builders<Counter>.Filter.Eq(c => c.Id, counterName);
                var update = Builders<Counter>.Update.Inc(c => c.Value, 1);
                var options = new FindOneAndUpdateOptions<Counter>
                {
                    IsUpsert = true,
                    ReturnDocument = ReturnDocument.After
                };
                var result = await counters.FindOneAndUpdateAsync(filter, update, options);
                return result.Value;
            }

            // Read each collection maximum once, then allocate IDs from memory. This
            // avoids rescanning every stored document for each new event or RSVP.
            await _jsonCounterLock.WaitAsync();
            try
            {
                if (!_jsonCounters.TryGetValue(counterName, out var current))
                {
                    current = counterName switch
                    {
                        "EventId" => await Events.GetMaxIdAsync(e => e.Id),
                        "RsvpId" => await RSVPs.GetMaxIdAsync(r => r.Id),
                        "AttendanceId" => await Attendances.GetMaxIdAsync(a => a.Id),
                        "CustomFieldId" => await GetMaxCustomFieldIdAsync(),
                        _ => throw new ArgumentException($"Unknown counter: {counterName}")
                    };
                }

                var next = checked(current + 1);
                _jsonCounters[counterName] = next;
                return next;
            }
            finally
            {
                _jsonCounterLock.Release();
            }
        }

        private async Task<int> GetMaxCustomFieldIdAsync()
        {
            var allEvents = await Events.FindAllAsync();
            var maxId = 0;
            foreach (var e in allEvents)
            {
                if (e.CustomFields != null && e.CustomFields.Any())
                {
                    var m = e.CustomFields.Max(cf => cf.Id);
                    if (m > maxId) maxId = m;
                }
            }
            return maxId;
        }

        public void Dispose()
        {
            // Dispose JSON document collections to flush pending writes
            (Events as IDisposable)?.Dispose();
            (RSVPs as IDisposable)?.Dispose();
            (Attendances as IDisposable)?.Dispose();
            (Users as IDisposable)?.Dispose();
            _jsonCounterLock.Dispose();
        }
    }
}
