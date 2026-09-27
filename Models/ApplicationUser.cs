using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace EventEase.Models
{
    /// <summary>
    /// Represents an authenticated Event Organizer in the EventEase system.
    /// Stored as a document in the NoSQL "users" collection.
    /// </summary>
    public class ApplicationUser
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserName { get; set; } = string.Empty;

        public string NormalizedUserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string NormalizedEmail { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<string> Roles { get; set; } = new List<string>();

        // Navigation property for events (loaded dynamically in memory)
        [BsonIgnore]
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
