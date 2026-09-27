using System;
using System.Collections.Generic;
using EventEase.ViewModels.Attendance;

namespace EventEase.ViewModels.Event
{
    public class EventDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Venue { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = "Upcoming";
        public DateTime CreatedAt { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;

        public int TotalRSVPs { get; set; }
        public int GoingCount { get; set; }
        public int MaybeCount { get; set; }
        public int NotGoingCount { get; set; }
        public int CheckedInCount { get; set; }
        public double AttendancePercentage { get; set; }

        public List<CustomFieldInputViewModel> CustomFields { get; set; } = new List<CustomFieldInputViewModel>();
        public List<AttendeeItemViewModel> Attendees { get; set; } = new List<AttendeeItemViewModel>();
    }
}
