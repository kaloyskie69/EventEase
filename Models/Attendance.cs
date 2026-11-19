using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    /// <summary>
    /// Represents the on-site event-day attendance verification record.
    /// </summary>
    public class Attendance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RSVPId { get; set; }

        [ForeignKey(nameof(RSVPId))]
        public virtual RSVP? RSVP { get; set; }

        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public virtual Event? Event { get; set; }

        public bool CheckedIn { get; set; } = false;

        public DateTime? CheckedInTime { get; set; }
    }
}
