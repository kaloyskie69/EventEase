using System.ComponentModel.DataAnnotations;

namespace EventEase.ViewModels.Event
{
    public class CustomFieldInputViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Question label is required.")]
        [StringLength(200, ErrorMessage = "Question label cannot exceed 200 characters.")]
        public string Label { get; set; } = string.Empty;

        [Required]
        public string FieldType { get; set; } = "Text"; // Text, Dropdown, Number, Checkbox

        public bool Required { get; set; } = false;

        public string? Options { get; set; } // Comma-separated options for dropdown
    }
}
