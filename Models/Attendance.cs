using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace EventEase.Models
{
    /// <summary>
    /// Represents the on-site event-day attendance verification record.
    /// Stored both as a document in the NoSQL "attendances" collection and embedded inside RSVP.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Attendance
    {
        [BsonId]
        public int Id { get; set; }

        public int RSVPId { get; set; }

        public int EventId { get; set; }

        [BsonIgnore]
        public virtual RSVP? RSVP { get; set; }

        public bool CheckedIn { get; set; } = false;

        public DateTime? CheckedInTime { get; set; }
    }
}
