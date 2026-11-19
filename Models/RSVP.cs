using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    /// <summary>
    /// Represents an attendee's RSVP response to an event.
    /// Attendees do not need an account.
    /// </summary>
    public class RSVP
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public virtual Event? Event { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(150, ErrorMessage = "Full Name cannot exceed 150 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(50)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Going"; // "Going", "Maybe", "Not Going"

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Relational Navigation: Custom answers submitted by the attendee.
        /// </summary>
        public virtual List<CustomFieldResponse> CustomFieldResponses { get; set; } = new List<CustomFieldResponse>();

        /// <summary>
        /// Relational Navigation: Linked Attendance check-in status.
        /// </summary>
        public virtual Attendance? Attendance { get; set; }
    }
}
