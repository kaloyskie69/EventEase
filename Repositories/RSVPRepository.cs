using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Data;
using EventEase.Interfaces;
using EventEase.Models;
using MongoDB.Driver;

namespace EventEase.Repositories
{
    /// <summary>
    /// NoSQL Repository implementation for RSVPs using MongoDB / Document collections.
    /// Custom field responses are embedded directly within each RSVP document.
    /// </summary>
    public class RSVPRepository : IRSVPRepository
    {
        private static readonly SemaphoreSlim SubmissionLock = new(1, 1);
        private readonly MongoDbContext _context;

        public RSVPRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RSVP>> GetByEventIdAsync(int eventId)
        {
            // Batch load: fetch RSVPs, event, and attendances in parallel
            var rsvpsTask = _context.RSVPs.FindAsync(r => r.EventId == eventId);
            var eventTask = _context.Events.FindOneAsync(e => e.Id == eventId);
            var attendancesTask = _context.Attendances.FindAsync(a => a.EventId == eventId);

            await Task.WhenAll(rsvpsTask, eventTask, attendancesTask);

            var rsvps = rsvpsTask.Result;
            var ev = eventTask.Result;
            var attendances = attendancesTask.Result;
            var attendanceDict = attendances.ToDictionary(a => a.RSVPId);

            // Single pass to link all related data — no N+1 lookups
            foreach (var rsvp in rsvps)
            {
                rsvp.Event = ev;
                if (rsvp.Attendance == null && attendanceDict.TryGetValue(rsvp.Id, out var att))
                {
                    rsvp.Attendance = att;
                }

                if (ev != null && rsvp.CustomFieldResponses != null)
                {
                    foreach (var resp in rsvp.CustomFieldResponses)
                    {
                        resp.CustomField = ev.CustomFields.FirstOrDefault(cf => cf.Id == resp.CustomFieldId);
                    }
                }
            }

            return rsvps.OrderByDescending(r => r.SubmittedAt).ToList();
        }

        public async Task<RSVP?> GetByIdAsync(int id)
        {
            return await _context.RSVPs.FindOneAsync(r => r.Id == id);
        }

        public async Task<RSVP?> GetByIdWithDetailsAsync(int id)
        {
            var rsvp = await _context.RSVPs.FindOneAsync(r => r.Id == id);
            if (rsvp == null) return null;

            var ev = await _context.Events.FindOneAsync(e => e.Id == rsvp.EventId);
            rsvp.Event = ev;

            if (rsvp.Attendance == null)
            {
                rsvp.Attendance = await _context.Attendances.FindOneAsync(a => a.RSVPId == id);
            }

            if (ev != null && rsvp.CustomFieldResponses != null)
            {
                foreach (var resp in rsvp.CustomFieldResponses)
                {
                    resp.CustomField = ev.CustomFields.FirstOrDefault(cf => cf.Id == resp.CustomFieldId);
                }
            }

            return rsvp;
        }

        public async Task<bool> ExistsByEmailAsync(int eventId, string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.RSVPs.FindOneAsync(r => r.EventId == eventId && r.Email == normalizedEmail) != null;
        }

        public async Task<RSVP> AddAsync(RSVP rsvp, IEnumerable<CustomFieldResponse>? responses = null)
        {
            await SubmissionLock.WaitAsync();
            try
            {
                if (await ExistsByEmailAsync(rsvp.EventId, rsvp.Email))
                {
                    throw new DuplicateRsvpException();
                }

                if (rsvp.Id <= 0)
                {
                    rsvp.Id = await _context.GetNextRsvpIdAsync();
                }

                if (responses != null && responses.Any())
                {
                    var nextId = 1;
                    foreach (var resp in responses)
                    {
                        resp.Id = nextId++;
                        resp.RSVPId = rsvp.Id;
                        rsvp.CustomFieldResponses.Add(resp);
                    }
                }

                // Create initial Attendance record (CheckedIn = false)
                var attendance = new Attendance
                {
                    Id = await _context.GetNextAttendanceIdAsync(),
                    RSVPId = rsvp.Id,
                    EventId = rsvp.EventId,
                    CheckedIn = false,
                    CheckedInTime = null
                };

                rsvp.Attendance = attendance;

                try
                {
                    await _context.RSVPs.InsertOneAsync(rsvp);
                }
                catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
                {
                    throw new DuplicateRsvpException();
                }
                await _context.Attendances.InsertOneAsync(attendance);

                return rsvp;
            }
            finally
            {
                SubmissionLock.Release();
            }
        }

        public async Task<int> GetCountByEventIdAsync(int eventId)
        {
            var count = await _context.RSVPs.CountDocumentsAsync(r => r.EventId == eventId);
            return (int)count;
        }

        public async Task<int> GetCountByStatusAsync(int eventId, string status)
        {
            var count = await _context.RSVPs.CountDocumentsAsync(r => r.EventId == eventId && r.Status == status);
            return (int)count;
        }
    }

    public sealed class DuplicateRsvpException : Exception
    {
        public DuplicateRsvpException() : base("An RSVP with this email has already been submitted for this event.") { }
    }
}
