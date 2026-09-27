using System;
using System.IO;
using System.Linq;
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
    public class MongoDbContext
    {
        public INoSqlCollection<Event> Events { get; private set; } = null!;
        public INoSqlCollection<RSVP> RSVPs { get; private set; } = null!;
        public INoSqlCollection<Attendance> Attendances { get; private set; } = null!;
        public INoSqlCollection<ApplicationUser> Users { get; private set; } = null!;

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
            var maxId = await Events.GetMaxIdAsync(e => e.Id);
            return maxId + 1;
        }

        public async Task<int> GetNextRsvpIdAsync()
        {
            var maxId = await RSVPs.GetMaxIdAsync(r => r.Id);
            return maxId + 1;
        }

        public async Task<int> GetNextAttendanceIdAsync()
        {
            var maxId = await Attendances.GetMaxIdAsync(a => a.Id);
            return maxId + 1;
        }

        public async Task<int> GetNextCustomFieldIdAsync()
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
            return maxId + 1;
        }
    }
}
