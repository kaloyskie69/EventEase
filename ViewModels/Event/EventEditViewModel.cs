using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventEase.ViewModels.Event
{
    public class EventEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event title is required.")]
        [StringLength(200, ErrorMessage = "Event title cannot exceed 200 characters.")]
        [Display(Name = "Event Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Venue / Location is required.")]
        [StringLength(250, ErrorMessage = "Venue cannot exceed 250 characters.")]
        [Display(Name = "Venue / Location")]
        public string Venue { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Event time is required.")]
        [StringLength(50)]
        [Display(Name = "Event Time")]
        public string Time { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than zero.")]
        [Display(Name = "Attendee Capacity (Optional)")]
        public int? Capacity { get; set; }

        [Required]
        [Display(Name = "Event Status")]
        public string Status { get; set; } = "Upcoming"; // Upcoming, Completed, Cancelled

        public List<CustomFieldInputViewModel> CustomFields { get; set; } = new List<CustomFieldInputViewModel>();
    }
}
