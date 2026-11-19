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
    /// Entity Framework Core repository implementation for Attendance tracking.
    /// </summary>
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Attendance?> GetByRsvpIdAsync(int rsvpId)
        {
            return await _context.Attendances
                .Include(a => a.RSVP)
                    .ThenInclude(r => r!.Event)
                .FirstOrDefaultAsync(a => a.RSVPId == rsvpId);
        }

        public async Task<Attendance> CheckInAsync(int rsvpId)
        {
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.RSVPId == rsvpId);
            if (attendance == null)
            {
                var rsvp = await _context.RSVPs.FirstOrDefaultAsync(r => r.Id == rsvpId);
                var eventId = rsvp?.EventId ?? 0;

                attendance = new Attendance
                {
                    RSVPId = rsvpId,
                    EventId = eventId,
                    CheckedIn = true,
                    CheckedInTime = DateTime.UtcNow
                };
                _context.Attendances.Add(attendance);
            }
            else
            {
                if (attendance.CheckedIn)
                {
                    return attendance;
                }

                attendance.CheckedIn = true;
                attendance.CheckedInTime = DateTime.UtcNow;
                _context.Attendances.Update(attendance);
            }

            await _context.SaveChangesAsync();
            return attendance;
        }

        public async Task<Attendance> UndoCheckInAsync(int rsvpId)
        {
            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.RSVPId == rsvpId);
            if (attendance != null)
            {
                attendance.CheckedIn = false;
                attendance.CheckedInTime = null;
                _context.Attendances.Update(attendance);
            }
            else
            {
                var rsvp = await _context.RSVPs.FirstOrDefaultAsync(r => r.Id == rsvpId);
                var eventId = rsvp?.EventId ?? 0;

                attendance = new Attendance
                {
                    RSVPId = rsvpId,
                    EventId = eventId,
                    CheckedIn = false,
                    CheckedInTime = null
                };
                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();
            return attendance;
        }

        public async Task<int> GetCheckedInCountByEventIdAsync(int eventId)
        {
            return await _context.Attendances.CountAsync(a => a.EventId == eventId && a.CheckedIn);
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesByEventIdAsync(int eventId)
        {
            return await _context.Attendances
                .Include(a => a.RSVP)
                .Where(a => a.EventId == eventId)
                .ToListAsync();
        }
    }
}
