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
    /// NoSQL Repository implementation for Attendance tracking using MongoDB / Document collections.
    /// Synchronizes attendance status across both Attendances collection and embedded RSVP document.
    /// </summary>
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly MongoDbContext _context;

        public AttendanceRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Attendance?> GetByRsvpIdAsync(int rsvpId)
        {
            var attendance = await _context.Attendances.FindOneAsync(a => a.RSVPId == rsvpId);
            if (attendance != null)
            {
                var rsvp = await _context.RSVPs.FindOneAsync(r => r.Id == rsvpId);
                if (rsvp != null)
                {
                    rsvp.Event = await _context.Events.FindOneAsync(e => e.Id == rsvp.EventId);
                    attendance.RSVP = rsvp;
                }
            }
            return attendance;
        }

        public async Task<Attendance> CheckInAsync(int rsvpId)
        {
            // Optimistic concurrency: fetch, validate state, then atomic write
            var attendance = await _context.Attendances.FindOneAsync(a => a.RSVPId == rsvpId);
            var rsvp = await _context.RSVPs.FindOneAsync(r => r.Id == rsvpId);
            var eventId = rsvp?.EventId ?? 0;

            if (attendance == null)
            {
                // First check-in: create new record
                attendance = new Attendance
                {
                    Id = await _context.GetNextAttendanceIdAsync(),
                    RSVPId = rsvpId,
                    EventId = eventId,
                    CheckedIn = true,
                    CheckedInTime = DateTime.UtcNow
                };
                await _context.Attendances.InsertOneAsync(attendance);
            }
            else
            {
                // Subsequent check-in: verify not already checked in to prevent race
                if (attendance.CheckedIn)
                {
                    // Already checked in by concurrent request — return current state
                    return attendance;
                }

                attendance.CheckedIn = true;
                attendance.CheckedInTime = DateTime.UtcNow;
                if (attendance.EventId == 0 && eventId > 0)
                {
                    attendance.EventId = eventId;
                }
                await _context.Attendances.ReplaceOneAsync(a => a.RSVPId == rsvpId, attendance);
            }

            // Sync embedded attendance on RSVP document
            if (rsvp != null)
            {
                rsvp.Attendance = attendance;
                await _context.RSVPs.ReplaceOneAsync(r => r.Id == rsvpId, rsvp);
            }

            return attendance;
        }

        public async Task<Attendance> UndoCheckInAsync(int rsvpId)
        {
            var attendance = await _context.Attendances.FindOneAsync(a => a.RSVPId == rsvpId);
            var rsvp = await _context.RSVPs.FindOneAsync(r => r.Id == rsvpId);
            var eventId = rsvp?.EventId ?? 0;

            if (attendance != null)
            {
                attendance.CheckedIn = false;
                attendance.CheckedInTime = null;
                await _context.Attendances.ReplaceOneAsync(a => a.RSVPId == rsvpId, attendance);
            }
            else
            {
                attendance = new Attendance
                {
                    Id = await _context.GetNextAttendanceIdAsync(),
                    RSVPId = rsvpId,
                    EventId = eventId,
                    CheckedIn = false,
                    CheckedInTime = null
                };
                await _context.Attendances.InsertOneAsync(attendance);
            }

            // Sync embedded attendance on RSVP document
            if (rsvp != null)
            {
                rsvp.Attendance = attendance;
                await _context.RSVPs.ReplaceOneAsync(r => r.Id == rsvpId, rsvp);
            }

            return attendance;
        }

        public async Task<int> GetCheckedInCountByEventIdAsync(int eventId)
        {
            var count = await _context.Attendances.CountDocumentsAsync(a => a.EventId == eventId && a.CheckedIn);
            if (count == 0)
            {
                // Fallback check via RSVPs
                var rsvps = await _context.RSVPs.FindAsync(r => r.EventId == eventId);
                return rsvps.Count(r => r.Attendance != null && r.Attendance.CheckedIn);
            }
            return (int)count;
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesByEventIdAsync(int eventId)
        {
            var attendances = await _context.Attendances.FindAsync(a => a.EventId == eventId);
            if (!attendances.Any())
            {
                var rsvps = await _context.RSVPs.FindAsync(r => r.EventId == eventId);
                var list = new List<Attendance>();
                foreach (var r in rsvps)
                {
                    if (r.Attendance != null) list.Add(r.Attendance);
                }
                return list;
            }
            return attendances;
        }
    }
}
