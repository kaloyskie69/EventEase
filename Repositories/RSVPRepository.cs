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
    /// Entity Framework Core repository implementation for RSVPs.
    /// </summary>
    public class RSVPRepository : IRSVPRepository
    {
        private readonly ApplicationDbContext _context;

        public RSVPRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RSVP>> GetByEventIdAsync(int eventId)
        {
            return await _context.RSVPs
                .Where(r => r.EventId == eventId)
                .Include(r => r.Event)
                    .ThenInclude(e => e!.CustomFields)
                .Include(r => r.Attendance)
                .Include(r => r.CustomFieldResponses)
                    .ThenInclude(cfr => cfr.CustomField)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();
        }

        public async Task<RSVP?> GetByIdAsync(int id)
        {
            return await _context.RSVPs.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<RSVP?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.RSVPs
                .Include(r => r.Event)
                    .ThenInclude(e => e!.CustomFields)
                .Include(r => r.Attendance)
                .Include(r => r.CustomFieldResponses)
                    .ThenInclude(cfr => cfr.CustomField)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> ExistsByEmailAsync(int eventId, string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.RSVPs.AnyAsync(r => r.EventId == eventId && r.Email.ToLower() == normalizedEmail);
        }

        public async Task<RSVP> AddAsync(RSVP rsvp, IEnumerable<CustomFieldResponse>? responses = null)
        {
            if (await ExistsByEmailAsync(rsvp.EventId, rsvp.Email))
            {
                throw new DuplicateRsvpException();
            }

            if (responses != null && responses.Any())
            {
                foreach (var resp in responses)
                {
                    resp.RSVP = rsvp;
                    rsvp.CustomFieldResponses.Add(resp);
                }
            }

            // Create initial Attendance record (CheckedIn = false)
            var attendance = new Attendance
            {
                RSVP = rsvp,
                EventId = rsvp.EventId,
                CheckedIn = false,
                CheckedInTime = null
            };
            rsvp.Attendance = attendance;

            _context.RSVPs.Add(rsvp);
            _context.Attendances.Add(attendance);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new DuplicateRsvpException();
            }

            return rsvp;
        }

        public async Task<int> GetCountByEventIdAsync(int eventId)
        {
            return await _context.RSVPs.CountAsync(r => r.EventId == eventId);
        }

        public async Task<int> GetCountByStatusAsync(int eventId, string status)
        {
            return await _context.RSVPs.CountAsync(r => r.EventId == eventId && r.Status == status);
        }
    }

    public sealed class DuplicateRsvpException : Exception
    {
        public DuplicateRsvpException() : base("An RSVP with this email has already been submitted for this event.") { }
    }
}
