using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace EventEase.Models
{
    /// <summary>
    /// Stores the response to a specific custom question submitted during RSVP.
    /// In NoSQL, this is stored as an embedded document inside the RSVP document.
    /// </summary>
    public class CustomFieldResponse
    {
        public int Id { get; set; }

        public int RSVPId { get; set; }

        public int CustomFieldId { get; set; }

        [BsonIgnore]
        public virtual CustomField? CustomField { get; set; }

        [StringLength(2000)]
        public string? Response { get; set; }
    }
}
