using System.Collections.Generic;

namespace EventEase.ViewModels.RSVP
{
    public class CustomFieldAnswerViewModel
    {
        public int CustomFieldId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string FieldType { get; set; } = "Text"; // Text, Dropdown, Number, Checkbox
        public bool Required { get; set; }
        public string? Options { get; set; }
        public List<string> ParsedOptions => string.IsNullOrWhiteSpace(Options) 
            ? new List<string>() 
            : new List<string>(Options.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries));

        public string? Response { get; set; }
    }
}
