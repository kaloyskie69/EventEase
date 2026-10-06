using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Data;
using EventEase.Interfaces;
using EventEase.Models;

namespace EventEase.Repositories
{
    /// <summary>
    /// NoSQL Repository implementation for Events using MongoDB / Document collections.
    /// Event custom fields are stored as embedded documents inside each Event document.
    /// </summary>
    public class EventRepository : IEventRepository
    {
        private readonly MongoDbContext _context;

        public EventRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetAllByOrganizerAsync(string organizerId)
        {
            var events = await _context.Events.FindAsync(e => e.OrganizerId == organizerId);
            if (events == null) return new List<Event>();

            var sorted = events.OrderByDescending(e => e.Date).ToList();

            if (!sorted.Any()) return sorted;

            // Auto-transition past events to Completed status
            var today = DateTime.Today;
            var eventsToUpdate = sorted.Where(e => e.Status == "Upcoming" && e.Date.Date < today).ToList();
            foreach (var ev in eventsToUpdate)
            {
                ev.Status = "Completed";
                await _context.Events.ReplaceOneAsync(e => e.Id == ev.Id, ev);
            }

            var eventIds = sorted.Select(e => e.Id).ToHashSet();
            var allRsvps = await _context.RSVPs.FindAsync(r => eventIds.Contains(r.EventId));
            var allAttendances = await _context.Attendances.FindAsync(a => eventIds.Contains(a.EventId));

            var attendanceDict = (allAttendances ?? new()).ToDictionary(a => a.RSVPId);
            var rsvpGroups = (allRsvps ?? new()).GroupBy(r => r.EventId).ToDictionary(g => g.Key, g => g.ToList());

            // Populate RSVPs and attendance in-memory with O(1) dictionary lookups
            foreach (var ev in sorted)
            {
                if (rsvpGroups.TryGetValue(ev.Id, out var rsvps))
                {
                    ev.RSVPs = rsvps;
                    foreach (var rsvp in ev.RSVPs)
                    {
                        if (rsvp.Attendance == null && attendanceDict.TryGetValue(rsvp.Id, out var att))
                        {
                            rsvp.Attendance = att;
                        }
                    }
                }
                else
                {
                    ev.RSVPs ??= new();
                }
            }

            return sorted;
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            return await _context.Events.FindOneAsync(e => e.Id == id);
        }

        public async Task<Event?> GetByIdWithDetailsAsync(int id)
        {
            var ev = await _context.Events.FindOneAsync(e => e.Id == id);
            if (ev == null) return null;

            // Load organizer
            if (!string.IsNullOrEmpty(ev.OrganizerId))
            {
                ev.Organizer = await _context.Users.FindOneAsync(u => u.Id == ev.OrganizerId);
            }

            // Load RSVPs for this event
            var rsvps = await _context.RSVPs.FindAsync(r => r.EventId == id);
            ev.RSVPs = rsvps?.OrderByDescending(r => r.SubmittedAt).ToList() ?? new();

            // Batch load attendances in 1 query instead of N individual queries
            var attendances = await _context.Attendances.FindAsync(a => a.EventId == id);
            var attendanceDict = attendances?.ToDictionary(a => a.RSVPId) ?? new();

            // Link Attendance and CustomField metadata to each RSVP response
            foreach (var rsvp in ev.RSVPs)
            {
                rsvp.Event = ev;
                if (rsvp.Attendance == null && attendanceDict.TryGetValue(rsvp.Id, out var att))
                {
                    rsvp.Attendance = att;
                }

                if (rsvp.CustomFieldResponses != null && ev.CustomFields != null)
                {
                    foreach (var resp in rsvp.CustomFieldResponses)
                    {
                        resp.CustomField = ev.CustomFields.FirstOrDefault(cf => cf.Id == resp.CustomFieldId);
                    }
                }
            }

            return ev;
        }

        public async Task<Event?> GetByIdWithCustomFieldsAsync(int id)
        {
            // In NoSQL MongoDB, CustomFields are already embedded directly in the Event document!
            return await _context.Events.FindOneAsync(e => e.Id == id);
        }

        public async Task<Event> AddAsync(Event ev)
        {
            if (ev.Id <= 0)
            {
                ev.Id = await _context.GetNextEventIdAsync();
            }

            if (ev.CustomFields != null && ev.CustomFields.Any())
            {
                foreach (var cf in ev.CustomFields)
                {
                    if (cf.Id <= 0)
                    {
                        cf.Id = await _context.GetNextCustomFieldIdAsync();
                    }
                    cf.EventId = ev.Id;
                }
            }

            await _context.Events.InsertOneAsync(ev);
            return ev;
        }

        public async Task UpdateAsync(Event ev)
        {
            if (ev.CustomFields != null && ev.CustomFields.Any())
            {
                foreach (var cf in ev.CustomFields)
                {
                    if (cf.Id <= 0)
                    {
                        cf.Id = await _context.GetNextCustomFieldIdAsync();
                    }
                    cf.EventId = ev.Id;
                }
            }

            await _context.Events.ReplaceOneAsync(e => e.Id == ev.Id, ev);
        }

        public async Task DeleteAsync(int id)
        {
            await _context.Events.DeleteOneAsync(e => e.Id == id);
            // Cascade delete in NoSQL:
            await _context.RSVPs.DeleteManyAsync(r => r.EventId == id);
            await _context.Attendances.DeleteManyAsync(a => a.EventId == id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            var ev = await _context.Events.FindOneAsync(e => e.Id == id);
            return ev != null;
        }

        public async Task<bool> ExistsWithTitleAndDateAsync(string organizerId, string title, DateTime date, int? excludeId = null)
        {
            var normalizedTitle = title.Trim().ToLowerInvariant();
            var matches = await _context.Events.FindAsync(e => 
                e.OrganizerId == organizerId && 
                e.Date.Date == date.Date &&
                e.NormalizedTitle == normalizedTitle);

            return matches.Any(e => !excludeId.HasValue || e.Id != excludeId.Value);
        }

        public async Task AddCustomFieldsAsync(IEnumerable<CustomField> customFields)
        {
            if (customFields == null || !customFields.Any()) return;

            var grouped = customFields.GroupBy(cf => cf.EventId);
            foreach (var group in grouped)
            {
                var ev = await _context.Events.FindOneAsync(e => e.Id == group.Key);
                if (ev != null)
                {
                    foreach (var cf in group)
                    {
                        if (cf.Id <= 0)
                        {
                            cf.Id = await _context.GetNextCustomFieldIdAsync();
                        }
                        cf.EventId = ev.Id;
                        ev.CustomFields.Add(cf);
                    }
                    await _context.Events.ReplaceOneAsync(e => e.Id == ev.Id, ev);
                }
            }
        }

        public async Task DeleteCustomFieldsByEventIdAsync(int eventId)
        {
            var ev = await _context.Events.FindOneAsync(e => e.Id == eventId);
            if (ev != null)
            {
                ev.CustomFields.Clear();
                await _context.Events.ReplaceOneAsync(e => e.Id == ev.Id, ev);
            }
        }
    }
}
