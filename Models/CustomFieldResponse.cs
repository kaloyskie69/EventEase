using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    /// <summary>
    /// Stores the response to a specific custom question submitted during RSVP.
    /// </summary>
    public class CustomFieldResponse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RSVPId { get; set; }

        [ForeignKey(nameof(RSVPId))]
        public virtual RSVP? RSVP { get; set; }

        public int CustomFieldId { get; set; }

        [ForeignKey(nameof(CustomFieldId))]
        public virtual CustomField? CustomField { get; set; }

        [StringLength(2000)]
        public string? Response { get; set; }
    }
}
