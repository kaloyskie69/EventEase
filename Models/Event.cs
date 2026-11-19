using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    /// <summary>
    /// Represents an Event created and managed by an Organizer.
    /// Stored in the relational database via Entity Framework Core.
    /// </summary>
    public class Event
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string OrganizerId { get; set; } = string.Empty;

        [ForeignKey(nameof(OrganizerId))]
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

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Upcoming"; // "Upcoming", "Completed", "Cancelled"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Relational Navigation: Custom questions for this specific event.
        /// </summary>
        public virtual List<CustomField> CustomFields { get; set; } = new List<CustomField>();

        /// <summary>
        /// Relational Navigation: RSVPs for this event.
        /// </summary>
        public virtual List<RSVP> RSVPs { get; set; } = new List<RSVP>();
    }
}
