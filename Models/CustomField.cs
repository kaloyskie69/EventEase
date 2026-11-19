using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    /// <summary>
    /// Represents a dynamic custom question associated with an Event
    /// (e.g., Dietary Restriction, Equipment Needed, Organization, Course, etc.)
    /// </summary>
    public class CustomField
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public virtual Event? Event { get; set; }

        [Required(ErrorMessage = "Field label is required.")]
        [StringLength(200, ErrorMessage = "Label cannot exceed 200 characters.")]
        public string Label { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FieldType { get; set; } = "Text"; // Text, Dropdown, Number, Checkbox

        public bool Required { get; set; } = false;

        /// <summary>
        /// Comma-separated options or selection items for dropdown fields.
        /// </summary>
        [StringLength(1000)]
        public string? Options { get; set; }
    }
}
