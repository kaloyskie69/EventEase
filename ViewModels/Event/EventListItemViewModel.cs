using System;

namespace EventEase.ViewModels.Event
{
    public class EventListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Venue { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = "Upcoming"; // Upcoming, Completed, Cancelled
        public DateTime CreatedAt { get; set; }

        public int TotalRSVPs { get; set; }
        public int GoingCount { get; set; }
        public int MaybeCount { get; set; }
        public int NotGoingCount { get; set; }
        public int CheckedInCount { get; set; }
        public double AttendancePercentage { get; set; }

        public bool IsToday => Date.Date == DateTime.Today;
        public bool IsPast => Date.Date < DateTime.Today;
        public bool CanCheckIn => Status != "Cancelled";
    }
}
