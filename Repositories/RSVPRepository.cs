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
        private static readonly SemaphoreSlim[] SubmissionLocks =
            Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
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
                        resp.CustomField = ev.CustomFields?.FirstOrDefault(cf => cf.Id == resp.CustomFieldId);
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
                    resp.CustomField = ev.CustomFields?.FirstOrDefault(cf => cf.Id == resp.CustomFieldId);
                }
            }

            return rsvp;
        }

        public async Task<bool> ExistsByEmailAsync(int eventId, string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.RSVPs.FindOneAsync(r => r.EventId == eventId && r.Email == normalizedEmail) != null;
        }

        public async Task<RSVP> AddAsync(RSVP rsvp, IEnumerable<CustomFieldResponse>? responses = null, int? capacity = null)
        {
            var submissionLock = GetSubmissionLock(rsvp.EventId);
            await submissionLock.WaitAsync();
            try
            {
                if (await ExistsByEmailAsync(rsvp.EventId, rsvp.Email))
                {
                    throw new DuplicateRsvpException();
                }

                if (rsvp.Status == "Going" && capacity.HasValue)
                {
                    rsvp.IsWaitlisted = await GetCountByStatusAsync(rsvp.EventId, "Going") >= capacity.Value;
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
                        (rsvp.CustomFieldResponses ??= new List<CustomFieldResponse>()).Add(resp);
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
                try
                {
                    await _context.Attendances.InsertOneAsync(attendance);
                }
                catch
                {
                    // Keep the two related collections consistent if attendance creation fails.
                    await _context.RSVPs.DeleteOneAsync(r => r.Id == rsvp.Id);
                    throw;
                }

                return rsvp;
            }
            finally
            {
                submissionLock.Release();
            }
        }

        public async Task<int> GetCountByEventIdAsync(int eventId)
        {
            var count = await _context.RSVPs.CountDocumentsAsync(r => r.EventId == eventId);
            return (int)count;
        }

        public async Task<int> GetCountByStatusAsync(int eventId, string status)
        {
            var count = await _context.RSVPs.CountDocumentsAsync(r => r.EventId == eventId && r.Status == status && (status != "Going" || r.IsWaitlisted != true));
            return (int)count;
        }

        public async Task ReconcileCapacityAsync(int eventId, int? capacity)
        {
            var submissionLock = GetSubmissionLock(eventId);
            await submissionLock.WaitAsync();
            try
            {
                var going = (await _context.RSVPs.FindAsync(r => r.EventId == eventId && r.Status == "Going"))
                    .OrderBy(r => r.IsWaitlisted == true)
                    .ThenBy(r => r.SubmittedAt)
                    .ToList();
                var confirmedCount = capacity.HasValue ? Math.Min(capacity.Value, going.Count) : going.Count;

                for (var index = 0; index < going.Count; index++)
                {
                    var shouldWaitlist = index >= confirmedCount;
                    if ((going[index].IsWaitlisted == true) == shouldWaitlist) continue;

                    going[index].IsWaitlisted = shouldWaitlist;
                    await _context.RSVPs.ReplaceOneAsync(r => r.Id == going[index].Id, going[index]);
                }
            }
            finally
            {
                submissionLock.Release();
            }
        }

        private static SemaphoreSlim GetSubmissionLock(int eventId)
        {
            var stripe = (int)((uint)eventId % (uint)SubmissionLocks.Length);
            return SubmissionLocks[stripe];
        }
    }

    public sealed class DuplicateRsvpException : Exception
    {
        public DuplicateRsvpException() : base("An RSVP with this email has already been submitted for this event.") { }
    }
}
