using System.Collections.Generic;
using EventEase.ViewModels.Dashboard;
using EventEase.ViewModels.Event;

namespace EventEase.ViewModels.Reports
{
    public class EventReportViewModel
    {
        public int TotalEvents { get; set; }
        public int TotalRSVPs { get; set; }
        public int TotalCheckedIn { get; set; }
        public double OverallAttendanceRate { get; set; }

        public List<EventListItemViewModel> EventSummaries { get; set; } = new List<EventListItemViewModel>();
        public RsvpDistributionStatViewModel RsvpDistribution { get; set; } = new RsvpDistributionStatViewModel();
        public List<MonthlyEventStatViewModel> MonthlyTrends { get; set; } = new List<MonthlyEventStatViewModel>();
    }
}
