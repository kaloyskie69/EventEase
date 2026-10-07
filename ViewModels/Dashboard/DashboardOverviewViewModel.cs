using System;
using System.Collections.Generic;
using EventEase.ViewModels.Attendance;
using EventEase.ViewModels.Event;

namespace EventEase.ViewModels.Dashboard
{
    public class MonthlyEventStatViewModel
    {
        public string MonthLabel { get; set; } = string.Empty; // e.g. "Jan", "Feb"
        public int EventCount { get; set; }
        public int AttendanceCount { get; set; }
    }

    public class RsvpDistributionStatViewModel
    {
        public int GoingCount { get; set; }
        public int MaybeCount { get; set; }
        public int NotGoingCount { get; set; }
    }

    public class EventAttendanceStatViewModel
    {
        public string EventTitle { get; set; } = string.Empty;
        public int TotalRSVP { get; set; }
        public int CheckedIn { get; set; }
        public double Percentage { get; set; }
    }

    public class EventAttendanceChartItemViewModel
    {
        public string EventTitle { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Going { get; set; }
        public int? CheckedIn { get; set; }
        public double Turnout { get; set; }
        public int TotalRSVPs { get; set; }
        public bool IsUpcoming { get; set; }
    }

    public class NextEventCardViewModel
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int TotalRSVPs { get; set; }
        public int GoingCount { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string AttendeeName { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = "RSVP"; // "RSVP" or "CheckIn"
    }

    public class DashboardOverviewViewModel
    {
        public string OrganizerName { get; set; } = string.Empty;

        // Metric Cards
        public int TotalEvents { get; set; }
        public int UpcomingEventsCount { get; set; }
        public int TodayEventsCount { get; set; }
        public int PastEventsCount { get; set; }
        public int TotalRSVPs { get; set; }
        public int TotalCheckedIn { get; set; }
        public double OverallAttendanceRate { get; set; }

        // Lists
        public List<EventListItemViewModel> UpcomingEvents { get; set; } = new List<EventListItemViewModel>();
        public List<EventListItemViewModel> RecentEvents { get; set; } = new List<EventListItemViewModel>();
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new List<RecentActivityViewModel>();

        // Chart Data
        public List<MonthlyEventStatViewModel> MonthlyStats { get; set; } = new List<MonthlyEventStatViewModel>();
        public RsvpDistributionStatViewModel RsvpDistribution { get; set; } = new RsvpDistributionStatViewModel();
        public List<EventAttendanceStatViewModel> AttendanceByEvent { get; set; } = new List<EventAttendanceStatViewModel>();

        // Per-event attendance chart (all non-cancelled events, newest first)
        public List<EventAttendanceChartItemViewModel> AttendanceChartEvents { get; set; } = new List<EventAttendanceChartItemViewModel>();
        public int AttendanceChartTotalCount { get; set; }

        // Reports stat cards
        public int NoShowCount { get; set; }
        public int UpcomingMaybeCount { get; set; }
        public NextEventCardViewModel? NextEvent { get; set; }
    }
}
