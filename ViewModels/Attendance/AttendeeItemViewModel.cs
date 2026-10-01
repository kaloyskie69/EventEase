using System;
using System.Collections.Generic;

namespace EventEase.ViewModels.Attendance
{
    public class AttendeeItemViewModel
    {
        public int RsvpId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Status { get; set; } = "Going"; // Going, Maybe, Not Going
        public bool IsWaitlisted { get; set; }
        public DateTime SubmittedAt { get; set; }

        public bool CheckedIn { get; set; }
        public DateTime? CheckedInTime { get; set; }
        public string FormattedCheckInTime => CheckedInTime.HasValue ? CheckedInTime.Value.ToLocalTime().ToString("hh:mm tt") : "—";

        public Dictionary<string, string> CustomAnswers { get; set; } = new Dictionary<string, string>();
    }
}
