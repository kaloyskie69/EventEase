using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Data;
using EventEase.Interfaces;
using EventEase.Models;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Repositories
{
    /// <summary>
    /// Entity Framework Core repository implementation for Events.
    /// </summary>
    public class EventRepository : IEventRepository
    {
        private readonly ApplicationDbContext _context;

        public EventRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetAllByOrganizerAsync(string organizerId)
        {
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .Include(e => e.RSVPs)
                    .ThenInclude(r => r.Attendance)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            // Auto-transition past events to Completed status
            var today = DateTime.Today;
            bool statusChanged = false;
            foreach (var ev in events)
            {
                if (ev.Status == "Upcoming" && ev.Date.Date < today)
                {
                    ev.Status = "Completed";
                    statusChanged = true;
                }
            }

            if (statusChanged)
            {
                await _context.SaveChangesAsync();
            }

            return events;
        }

        public async Task<Event?> GetByIdAsync(int id)
        {
            return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.CustomFields)
                .Include(e => e.RSVPs)
                    .ThenInclude(r => r.Attendance)
                .Include(e => e.RSVPs)
                    .ThenInclude(r => r.CustomFieldResponses)
                        .ThenInclude(cfr => cfr.CustomField)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event?> GetByIdWithCustomFieldsAsync(int id)
        {
            return await _context.Events
                .Include(e => e.CustomFields)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event> AddAsync(Event ev)
        {
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            return ev;
        }

        public async Task UpdateAsync(Event ev)
        {
            _context.Events.Update(ev);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev != null)
            {
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Events.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> ExistsWithTitleAndDateAsync(string organizerId, string title, DateTime date, int? excludeId = null)
        {
            var normalizedTitle = title.Trim().ToLowerInvariant();
            return await _context.Events.AnyAsync(e =>
                e.OrganizerId == organizerId &&
                (!excludeId.HasValue || e.Id != excludeId.Value) &&
                e.Date.Date == date.Date &&
                (e.NormalizedTitle == normalizedTitle || e.Title.Trim().ToLower() == normalizedTitle));
        }

        public async Task AddCustomFieldsAsync(IEnumerable<CustomField> customFields)
        {
            if (customFields != null && customFields.Any())
            {
                _context.CustomFields.AddRange(customFields);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteCustomFieldsByEventIdAsync(int eventId)
        {
            var customFields = await _context.CustomFields
                .Where(cf => cf.EventId == eventId)
                .ToListAsync();

            if (customFields.Any())
            {
                _context.CustomFields.RemoveRange(customFields);
                await _context.SaveChangesAsync();
            }
        }
    }
}
