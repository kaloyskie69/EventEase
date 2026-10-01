using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace EventEase.Models
{
    /// <summary>
    /// Represents an Event created and managed by an Organizer.
    /// Stored as a document in the NoSQL "events" collection with embedded CustomFields.
    /// </summary>
    public class Event
    {
        [BsonId]
        public int Id { get; set; }

        [Required]
        public string OrganizerId { get; set; } = string.Empty;

        [BsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual ApplicationUser? Organizer { get; set; }

        [Required(ErrorMessage = "Event title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Lowercase-normalized title for efficient duplicate detection queries.
        /// Set automatically on create/update.
        /// </summary>
        [StringLength(200)]
        public string NormalizedTitle { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Venue / Location is required.")]
        [StringLength(250, ErrorMessage = "Venue cannot exceed 250 characters.")]
        public string Venue { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Event time is required.")]
        [StringLength(50)]
        public string Time { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than zero.")]
        public int? Capacity { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Upcoming"; // "Upcoming", "Completed", "Cancelled"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Embedded NoSQL Document Array: Custom questions for this specific event.
        /// NoSQL allows flexible, event-specific fields without database schema alterations.
        /// </summary>
        public virtual List<CustomField> CustomFields { get; set; } = new List<CustomField>();

        /// <summary>
        /// Related RSVPs associated in-memory for views and reporting.
        /// </summary>
        [BsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual List<RSVP> RSVPs { get; set; } = new List<RSVP>();
    }
}
