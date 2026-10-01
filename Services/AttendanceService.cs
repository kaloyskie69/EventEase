using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Interfaces;
using EventEase.ViewModels.Attendance;

namespace EventEase.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRSVPRepository _rsvpRepository;
        private readonly IAttendanceRepository _attendanceRepository;

        public AttendanceService(
            IEventRepository eventRepository,
            IRSVPRepository rsvpRepository,
            IAttendanceRepository attendanceRepository)
        {
            _eventRepository = eventRepository;
            _rsvpRepository = rsvpRepository;
            _attendanceRepository = attendanceRepository;
        }

        public async Task<AttendanceCheckInViewModel?> GetCheckInRosterAsync(int eventId, string? searchQuery = null, string? statusFilter = null)
        {
            var ev = await _eventRepository.GetByIdWithDetailsAsync(eventId);
            if (ev == null) return null;

            var rsvps = ev.RSVPs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                if (statusFilter == "CheckedIn")
                {
                    rsvps = rsvps.Where(r => r.Attendance != null && r.Attendance.CheckedIn);
                }
                else if (statusFilter == "NotCheckedIn")
                {
                    rsvps = rsvps.Where(r => r.Attendance == null || !r.Attendance.CheckedIn);
                }
                else
                {
                    rsvps = rsvps.Where(r => r.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim().ToLower();
                rsvps = rsvps.Where(r => r.FullName.ToLower().Contains(term) || r.Email.ToLower().Contains(term));
            }

            var totalRSVPs = ev.RSVPs.Count;
            var totalGoing = ev.RSVPs.Count(r => r.Status == "Going");
            var totalCheckedIn = ev.RSVPs.Count(r => r.Attendance != null && r.Attendance.CheckedIn);
            var percentage = totalGoing > 0 ? Math.Round((double)totalCheckedIn / totalGoing * 100, 1) : 0.0;

            var customLabels = ev.CustomFields.Select(cf => cf.Label).ToList();

            var attendeeItems = rsvps.OrderByDescending(r => r.Attendance != null && r.Attendance.CheckedIn)
                                     .ThenBy(r => r.FullName)
                                     .Select(r =>
            {
                var answers = new Dictionary<string, string>();
                foreach (var ans in r.CustomFieldResponses)
                {
                    if (ans.CustomField != null)
                    {
                        answers[ans.CustomField.Label] = ans.Response ?? "—";
                    }
                }

                return new AttendeeItemViewModel
                {
                    RsvpId = r.Id,
                    FullName = r.FullName,
                    Email = r.Email,
                    Phone = r.Phone,
                    Status = r.Status,
                    SubmittedAt = r.SubmittedAt,
                    CheckedIn = r.Attendance != null && r.Attendance.CheckedIn,
                    CheckedInTime = r.Attendance?.CheckedInTime,
                    CustomAnswers = answers
                };
            }).ToList();

            return new AttendanceCheckInViewModel
            {
                EventId = ev.Id,
                EventTitle = ev.Title,
                Venue = ev.Venue,
                EventDate = ev.Date,
                EventTime = ev.Time,
                EventStatus = ev.Status,
                TotalRSVPs = totalRSVPs,
                TotalGoing = totalGoing,
                TotalCheckedIn = totalCheckedIn,
                AttendancePercentage = percentage,
                Attendees = attendeeItems,
                CustomFieldLabels = customLabels
            };
        }

        public async Task<CheckInResultViewModel> ToggleCheckInAsync(int rsvpId, bool undo = false)
        {
            if (undo)
            {
                return await UndoCheckInAttendeeAsync(rsvpId);
            }
            return await CheckInAttendeeAsync(rsvpId);
        }

        public async Task<CheckInResultViewModel> CheckInAttendeeAsync(int rsvpId)
        {
            var rsvp = await _rsvpRepository.GetByIdAsync(rsvpId);
            if (rsvp == null)
            {
                return new CheckInResultViewModel
                {
                    Success = false,
                    Message = "Attendee RSVP record not found."
                };
            }

            var existingAttendance = await _attendanceRepository.GetByRsvpIdAsync(rsvpId);
            if (existingAttendance != null && existingAttendance.CheckedIn)
            {
                // Prevent duplicate check in - run count queries in parallel
                var dupTotalTask = _rsvpRepository.GetCountByEventIdAsync(rsvp.EventId);
                var dupGoingTask = _rsvpRepository.GetCountByStatusAsync(rsvp.EventId, "Going");
                var dupCheckedTask = _attendanceRepository.GetCheckedInCountByEventIdAsync(rsvp.EventId);
                await Task.WhenAll(dupTotalTask, dupGoingTask, dupCheckedTask);

                var dupRate = dupGoingTask.Result > 0 ? Math.Round((double)dupCheckedTask.Result / dupGoingTask.Result * 100, 1) : 0.0;

                return new CheckInResultViewModel
                {
                    Success = true,
                    Message = "Attendee was already checked in.",
                    RsvpId = rsvpId,
                    CheckedIn = true,
                    CheckedInTime = existingAttendance.CheckedInTime?.ToLocalTime().ToString("hh:mm tt"),
                    TotalRSVPs = dupTotalTask.Result,
                    TotalGoing = dupGoingTask.Result,
                    TotalCheckedIn = dupCheckedTask.Result,
                    AttendancePercentage = dupRate
                };
            }

            var updatedAttendance = await _attendanceRepository.CheckInAsync(rsvpId);

            // Run count queries in parallel
            var totalRSVPsTask = _rsvpRepository.GetCountByEventIdAsync(rsvp.EventId);
            var goingTask = _rsvpRepository.GetCountByStatusAsync(rsvp.EventId, "Going");
            var checkedInTask = _attendanceRepository.GetCheckedInCountByEventIdAsync(rsvp.EventId);
            await Task.WhenAll(totalRSVPsTask, goingTask, checkedInTask);

            var rate = goingTask.Result > 0 ? Math.Round((double)checkedInTask.Result / goingTask.Result * 100, 1) : 0.0;

            return new CheckInResultViewModel
            {
                Success = true,
                Message = $"{rsvp.FullName} successfully checked in!",
                RsvpId = rsvpId,
                CheckedIn = true,
                CheckedInTime = updatedAttendance.CheckedInTime?.ToLocalTime().ToString("hh:mm tt") ?? DateTime.Now.ToString("hh:mm tt"),
                TotalRSVPs = totalRSVPsTask.Result,
                TotalGoing = goingTask.Result,
                TotalCheckedIn = checkedInTask.Result,
                AttendancePercentage = rate
            };
        }

        public async Task<CheckInResultViewModel> UndoCheckInAttendeeAsync(int rsvpId)
        {
            var rsvp = await _rsvpRepository.GetByIdAsync(rsvpId);
            if (rsvp == null)
            {
                return new CheckInResultViewModel
                {
                    Success = false,
                    Message = "Attendee RSVP record not found."
                };
            }

            await _attendanceRepository.UndoCheckInAsync(rsvpId);

            // Run count queries in parallel
            var totalRSVPsTask = _rsvpRepository.GetCountByEventIdAsync(rsvp.EventId);
            var goingTask = _rsvpRepository.GetCountByStatusAsync(rsvp.EventId, "Going");
            var checkedInTask = _attendanceRepository.GetCheckedInCountByEventIdAsync(rsvp.EventId);
            await Task.WhenAll(totalRSVPsTask, goingTask, checkedInTask);

            var rate = goingTask.Result > 0 ? Math.Round((double)checkedInTask.Result / goingTask.Result * 100, 1) : 0.0;

            return new CheckInResultViewModel
            {
                Success = true,
                Message = $"Check-in undone for {rsvp.FullName}.",
                RsvpId = rsvpId,
                CheckedIn = false,
                CheckedInTime = null,
                TotalRSVPs = totalRSVPsTask.Result,
                TotalGoing = goingTask.Result,
                TotalCheckedIn = checkedInTask.Result,
                AttendancePercentage = rate
            };
        }
    }
}
