using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Interfaces;
using EventEase.Models;
using EventEase.ViewModels.Attendance;
using EventEase.ViewModels.Event;

namespace EventEase.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<IEnumerable<EventListItemViewModel>> GetEventsByOrganizerAsync(string organizerId, string? statusFilter = null, string? searchQuery = null)
        {
            var events = await _eventRepository.GetAllByOrganizerAsync(organizerId);

            var query = events.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                query = query.Where(e => e.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim().ToLowerInvariant();
                query = query.Where(e =>
                    e.Title.ToLowerInvariant().Contains(term) ||
                    e.Venue.ToLowerInvariant().Contains(term));
            }

            return query.Select(e =>
            {
                // Cache RSVP list and counts to avoid multiple enumerations
                var rsvpList = e.RSVPs ?? new List<RSVP>();
                var total = rsvpList.Count;
                var going = rsvpList.Count(r => r.Status == "Going");
                var maybe = rsvpList.Count(r => r.Status == "Maybe");
                var notGoing = rsvpList.Count(r => r.Status == "Not Going");
                var checkedIn = rsvpList.Count(r => r.Attendance != null && r.Attendance.CheckedIn);
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
                    CreatedAt = e.CreatedAt,
                    TotalRSVPs = total,
                    GoingCount = going,
                    MaybeCount = maybe,
                    NotGoingCount = notGoing,
                    CheckedInCount = checkedIn,
                    AttendancePercentage = rate
                };
            }).ToList();
        }

        public async Task<EventDetailsViewModel?> GetEventDetailsAsync(int id, string? organizerId = null, string? baseUrl = null)
        {
            var ev = await _eventRepository.GetByIdWithDetailsAsync(id);
            if (ev == null) return null;
            if (!string.IsNullOrEmpty(organizerId) && ev.OrganizerId != organizerId) return null;

            // Cache RSVP list to avoid multiple enumerations
            var rsvpList = ev.RSVPs ?? new List<RSVP>();
            var going = rsvpList.Count(r => r.Status == "Going");
            var checkedIn = rsvpList.Count(r => r.Attendance != null && r.Attendance.CheckedIn);
            var rate = going > 0 ? Math.Round((double)checkedIn / Math.Max(going, checkedIn) * 100, 1) : (checkedIn > 0 ? 100.0 : 0.0);

            var attendees = rsvpList.OrderByDescending(r => r.SubmittedAt).Select(r =>
            {
                var answers = new Dictionary<string, string>();
                if (r.CustomFieldResponses != null)
                {
                    foreach (var ans in r.CustomFieldResponses)
                    {
                        if (ans.CustomField != null)
                        {
                            answers[ans.CustomField.Label] = ans.Response ?? "—";
                        }
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

            var customFields = (ev.CustomFields ?? new()).Select(cf => new CustomFieldInputViewModel
            {
                Id = cf.Id,
                Label = cf.Label,
                FieldType = cf.FieldType,
                Required = cf.Required,
                Options = cf.Options
            }).ToList();

            var publicUrl = !string.IsNullOrWhiteSpace(baseUrl) 
                ? $"{baseUrl.TrimEnd('/')}/RSVP/{ev.Id}" 
                : $"/RSVP/{ev.Id}";

            return new EventDetailsViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Venue = ev.Venue,
                Date = ev.Date,
                Time = ev.Time,
                Status = ev.Status,
                CreatedAt = ev.CreatedAt,
                OrganizerName = ev.Organizer?.FullName ?? "Event Organizer",
                PublicUrl = publicUrl,
                TotalRSVPs = rsvpList.Count,
                GoingCount = going,
                MaybeCount = rsvpList.Count(r => r.Status == "Maybe"),
                NotGoingCount = rsvpList.Count(r => r.Status == "Not Going"),
                CheckedInCount = checkedIn,
                AttendancePercentage = rate,
                CustomFields = customFields,
                Attendees = attendees
            };
        }

        public async Task<EventEditViewModel?> GetEventForEditAsync(int id, string organizerId)
        {
            var ev = await _eventRepository.GetByIdWithCustomFieldsAsync(id);
            if (ev == null || ev.OrganizerId != organizerId) return null;

            return new EventEditViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Venue = ev.Venue,
                Date = ev.Date,
                Time = ev.Time,
                Status = ev.Status,
                CustomFields = ev.CustomFields.Select(cf => new CustomFieldInputViewModel
                {
                    Id = cf.Id,
                    Label = cf.Label,
                    FieldType = cf.FieldType,
                    Required = cf.Required,
                    Options = cf.Options
                }).ToList()
            };
        }

        public async Task<Event> CreateEventAsync(string organizerId, EventCreateViewModel model)
        {
            var newEvent = new Event
            {
                OrganizerId = organizerId,
                Title = model.Title.Trim(),
                NormalizedTitle = model.Title.Trim().ToLowerInvariant(),
                Description = model.Description?.Trim(),
                Venue = model.Venue.Trim(),
                Date = model.Date.Date,
                Time = model.Time.Trim(),
                Status = "Upcoming",
                CreatedAt = DateTime.UtcNow
            };

            var createdEvent = await _eventRepository.AddAsync(newEvent);

            if (model.CustomFields != null && model.CustomFields.Any())
            {
                var customFieldsToAdd = model.CustomFields
                    .Where(cf => !string.IsNullOrWhiteSpace(cf.Label))
                    .Select(cf => new CustomField
                    {
                        EventId = createdEvent.Id,
                        Label = cf.Label.Trim(),
                        FieldType = string.IsNullOrWhiteSpace(cf.FieldType) ? "Text" : cf.FieldType,
                        Required = cf.Required,
                        Options = cf.Options?.Trim()
                    }).ToList();

                if (customFieldsToAdd.Any())
                {
                    await _eventRepository.AddCustomFieldsAsync(customFieldsToAdd);
                }
            }

            return createdEvent;
        }

        public async Task<bool> UpdateEventAsync(string organizerId, EventEditViewModel model)
        {
            var ev = await _eventRepository.GetByIdWithCustomFieldsAsync(model.Id);
            if (ev == null || ev.OrganizerId != organizerId) return false;

            ev.Title = model.Title.Trim();
            ev.NormalizedTitle = model.Title.Trim().ToLowerInvariant();
            ev.Description = model.Description?.Trim();
            ev.Venue = model.Venue.Trim();
            ev.Date = model.Date.Date;
            ev.Time = model.Time.Trim();
            ev.Status = model.Status;

            await _eventRepository.UpdateAsync(ev);

            // Re-sync custom fields
            await _eventRepository.DeleteCustomFieldsByEventIdAsync(ev.Id);
            if (model.CustomFields != null && model.CustomFields.Any())
            {
                var customFieldsToAdd = model.CustomFields
                    .Where(cf => !string.IsNullOrWhiteSpace(cf.Label))
                    .Select(cf => new CustomField
                    {
                        Id = cf.Id > 0 ? cf.Id : 0,
                        EventId = ev.Id,
                        Label = cf.Label.Trim(),
                        FieldType = string.IsNullOrWhiteSpace(cf.FieldType) ? "Text" : cf.FieldType,
                        Required = cf.Required,
                        Options = cf.Options?.Trim()
                    }).ToList();

                if (customFieldsToAdd.Any())
                {
                    await _eventRepository.AddCustomFieldsAsync(customFieldsToAdd);
                }
            }

            return true;
        }

        public async Task<bool> CancelEventAsync(int id, string organizerId)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null || ev.OrganizerId != organizerId) return false;

            ev.Status = "Cancelled";
            await _eventRepository.UpdateAsync(ev);
            return true;
        }

        public async Task<bool> DeleteEventAsync(int id, string organizerId)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null || ev.OrganizerId != organizerId) return false;

            await _eventRepository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> IsTitleDuplicateAsync(string organizerId, string title, DateTime date, int? excludeId = null)
        {
            return await _eventRepository.ExistsWithTitleAndDateAsync(organizerId, title.Trim(), date, excludeId);
        }
    }
}
