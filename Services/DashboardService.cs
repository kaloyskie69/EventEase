using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Interfaces;
using EventEase.ViewModels.Dashboard;
using EventEase.ViewModels.Event;
using EventEase.ViewModels.Reports;

namespace EventEase.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IEventRepository _eventRepository;

        public DashboardService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<DashboardOverviewViewModel> GetDashboardOverviewAsync(string organizerId)
        {
            var events = (await _eventRepository.GetAllByOrganizerAsync(organizerId)).ToList();
            return BuildDashboardOverview(events);
        }

        public async Task<EventReportViewModel> GetReportAnalyticsAsync(string organizerId)
        {
            var events = (await _eventRepository.GetAllByOrganizerAsync(organizerId)).ToList();
            var overview = BuildDashboardOverview(events);
            var eventSummaries = events.Select(e => MapToListItem(e)).ToList();

            return new EventReportViewModel
            {
                TotalEvents = overview.TotalEvents,
                TotalRSVPs = overview.TotalRSVPs,
                TotalCheckedIn = overview.TotalCheckedIn,
                OverallAttendanceRate = overview.OverallAttendanceRate,
                EventSummaries = eventSummaries,
                RsvpDistribution = overview.RsvpDistribution,
                MonthlyTrends = overview.MonthlyStats
            };
        }

        private static DashboardOverviewViewModel BuildDashboardOverview(List<Models.Event> events)
        {
            var today = DateTime.Today;

            var totalEvents = events.Count;

            // Single-pass counting to avoid multiple enumerations of the events list
            var upcomingCount = 0;
            var todayCount = 0;
            var pastCount = 0;
            foreach (var e in events)
            {
                if (e.Date.Date >= today && e.Status != "Cancelled") upcomingCount++;
                if (e.Date.Date == today && e.Status != "Cancelled") todayCount++;
                if (e.Date.Date < today || e.Status == "Completed") pastCount++;
            }

            // Materialize RSVPs once, then count in a single pass
            var allRsvps = events.SelectMany(e => e.RSVPs).ToList();
            var totalRsvps = allRsvps.Count;
            var goingRsvps = 0;
            var maybeRsvps = 0;
            var notGoingRsvps = 0;
            var totalCheckedIn = 0;
            foreach (var r in allRsvps)
            {
                switch (r.Status)
                {
                    case "Going": if (!r.IsWaitlisted) goingRsvps++; break;
                    case "Maybe": maybeRsvps++; break;
                    case "Not Going": notGoingRsvps++; break;
                }
                if (r.Attendance != null && r.Attendance.CheckedIn) totalCheckedIn++;
            }
            var overallAttendanceRate = goingRsvps > 0 ? Math.Round((double)totalCheckedIn / goingRsvps * 100, 1) : 0.0;

            // Map list view models
            var upcomingList = events.Where(e => e.Date.Date >= today && e.Status != "Cancelled")
                                     .OrderBy(e => e.Date).Take(5).Select(e => MapToListItem(e)).ToList();
            var recentList = events.OrderByDescending(e => e.CreatedAt).Take(5).Select(e => MapToListItem(e)).ToList();

            // Recent Activity Feed (combining recent RSVPs and Check-ins)
            var recentActivities = new List<RecentActivityViewModel>();
            foreach (var ev in events)
            {
                foreach (var r in ev.RSVPs)
                {
                    recentActivities.Add(new RecentActivityViewModel
                    {
                        AttendeeName = r.FullName,
                        EventTitle = ev.Title,
                        Status = r.Status,
                        Timestamp = r.SubmittedAt,
                        ActivityType = "RSVP"
                    });

                    if (r.Attendance != null && r.Attendance.CheckedIn && r.Attendance.CheckedInTime.HasValue)
                    {
                        recentActivities.Add(new RecentActivityViewModel
                        {
                            AttendeeName = r.FullName,
                            EventTitle = ev.Title,
                            Status = "Checked In",
                            Timestamp = r.Attendance.CheckedInTime.Value,
                            ActivityType = "CheckIn"
                        });
                    }
                }
            }
            recentActivities = recentActivities.OrderByDescending(a => a.Timestamp).Take(8).ToList();

            // Monthly breakdown (Past 6 months + next month)
            var monthlyStats = new List<MonthlyEventStatViewModel>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                var monthLabel = monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture);

                var eventsInMonth = events.Where(e => e.Date.Year == monthDate.Year && e.Date.Month == monthDate.Month).ToList();
                var attendeesInMonth = eventsInMonth
                    .SelectMany(e => e.RSVPs)
                    .Count(r => r.Attendance != null && r.Attendance.CheckedIn);

                monthlyStats.Add(new MonthlyEventStatViewModel
                {
                    MonthLabel = monthLabel,
                    EventCount = eventsInMonth.Count,
                    AttendanceCount = attendeesInMonth
                });
            }

            // Per event attendance comparison (top 5 recent events with RSVPs)
            var attendanceByEvent = events
                .Where(e => e.RSVPs.Any())
                .OrderByDescending(e => e.Date)
                .Take(5)
                .Select(e =>
                {
                    var going = e.RSVPs.Count(r => r.Status == "Going" && !r.IsWaitlisted);
                    var checkedIn = e.RSVPs.Count(r => r.Attendance != null && r.Attendance.CheckedIn);
                    return new EventAttendanceStatViewModel
                    {
                        EventTitle = e.Title.Length > 20 ? e.Title.Substring(0, 17) + "..." : e.Title,
                        TotalRSVP = e.RSVPs.Count,
                        CheckedIn = checkedIn,
                        Percentage = going > 0 ? Math.Round((double)checkedIn / going * 100, 1) : 0.0
                    };
                }).ToList();

            return new DashboardOverviewViewModel
            {
                TotalEvents = totalEvents,
                UpcomingEventsCount = upcomingCount,
                TodayEventsCount = todayCount,
                PastEventsCount = pastCount,
                TotalRSVPs = totalRsvps,
                TotalCheckedIn = totalCheckedIn,
                OverallAttendanceRate = overallAttendanceRate,
                UpcomingEvents = upcomingList,
                RecentEvents = recentList,
                RecentActivities = recentActivities,
                MonthlyStats = monthlyStats,
                RsvpDistribution = new RsvpDistributionStatViewModel
                {
                    GoingCount = goingRsvps,
                    MaybeCount = maybeRsvps,
                    NotGoingCount = notGoingRsvps
                },
                AttendanceByEvent = attendanceByEvent
            };
        }

        private static EventListItemViewModel MapToListItem(Models.Event e)
        {
            // Single-pass counting instead of 5 separate Count() enumerations
            var total = 0;
            var going = 0;
            var maybe = 0;
            var notGoing = 0;
            var checkedIn = 0;
            foreach (var r in e.RSVPs)
            {
                total++;
                switch (r.Status)
                {
                    case "Going": if (!r.IsWaitlisted) going++; break;
                    case "Maybe": maybe++; break;
                    case "Not Going": notGoing++; break;
                }
                if (r.Attendance != null && r.Attendance.CheckedIn) checkedIn++;
            }
            var rate = going > 0 ? Math.Round((double)checkedIn / going * 100, 1) : 0.0;

            return new EventListItemViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Venue = e.Venue,
                Date = e.Date,
                Time = e.Time,
                Status = e.Status,
                Capacity = e.Capacity,
                CreatedAt = e.CreatedAt,
                TotalRSVPs = total,
                WaitlistedCount = e.RSVPs.Count(r => r.IsWaitlisted),
                GoingCount = going,
                MaybeCount = maybe,
                NotGoingCount = notGoing,
                CheckedInCount = checkedIn,
                AttendancePercentage = rate
            };
        }
    }
}
