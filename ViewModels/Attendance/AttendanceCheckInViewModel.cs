using System;
using System.Collections.Generic;

namespace EventEase.ViewModels.Attendance
{
    public class AttendanceCheckInViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventTime { get; set; } = string.Empty;
        public string EventStatus { get; set; } = "Upcoming";

        public int TotalRSVPs { get; set; }
        public int TotalGoing { get; set; }
        public int TotalCheckedIn { get; set; }
        public double AttendancePercentage { get; set; }

        public List<AttendeeItemViewModel> Attendees { get; set; } = new List<AttendeeItemViewModel>();
        public List<string> CustomFieldLabels { get; set; } = new List<string>();
    }
}
