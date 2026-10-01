using System;
using System.Text;
using System.Threading.Tasks;
using EventEase.Interfaces;
using EventEase.ViewModels.RSVP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventEase.Controllers
{
    [AllowAnonymous]
    public class RSVPController : Controller
    {
        private readonly IRSVPService _rsvpService;
        private readonly IEventRepository _eventRepository;

        public RSVPController(IRSVPService rsvpService, IEventRepository eventRepository)
        {
            _rsvpService = rsvpService;
            _eventRepository = eventRepository;
        }

        [HttpGet]
        [Route("RSVP/{id:int}", Order = 1)]
        [Route("Event/{id:int}", Order = 2)]
        public async Task<IActionResult> EventDetails(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var landing = await _rsvpService.GetPublicLandingAsync(id);
            if (landing == null)
            {
                return View("NotFound");
            }

            return View(landing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("rsvp-submit")]
        [Route("RSVP/Submit")]
        public async Task<IActionResult> Submit()
        {
            var model = new RSVPSubmitViewModel();

            // Detect if form was posted with "SubmitForm." prefix (from EventDetails.cshtml view)
            bool hasPrefix = Request.Form.Keys.Any(k => k.StartsWith("SubmitForm.", StringComparison.OrdinalIgnoreCase));
            string prefix = hasPrefix ? "SubmitForm" : string.Empty;

            await TryUpdateModelAsync(model, prefix);

            // Ensure EventId is captured even if prefix resolution differed
            if (model.EventId <= 0)
            {
                if (int.TryParse(Request.Form["SubmitForm.EventId"], out int eid1) && eid1 > 0)
                {
                    model.EventId = eid1;
                }
                else if (int.TryParse(Request.Form["EventId"], out int eid2) && eid2 > 0)
                {
                    model.EventId = eid2;
                }
            }

            // Server-side duplicate RSVP check
            if (model.EventId > 0 && !string.IsNullOrWhiteSpace(model.Email))
            {
                var isDuplicate = await _rsvpService.IsDuplicateEmailAsync(model.EventId, model.Email);
                if (isDuplicate)
                {
                    var emailErrorKey = hasPrefix ? "SubmitForm.Email" : nameof(model.Email);
                    ModelState.AddModelError(emailErrorKey, $"An RSVP with email '{model.Email}' has already been submitted for this event.");
                }
            }

            if (!ModelState.IsValid || model.EventId <= 0)
            {
                // Reload event details for re-rendering form with validation errors
                if (model.EventId > 0)
                {
                    var landing = await _rsvpService.GetPublicLandingAsync(model.EventId);
                    if (landing != null)
                    {
                        landing.SubmitForm = model;
                        return View("EventDetails", landing);
                    }
                }

                TempData["ErrorMessage"] = "The event could not be found or the RSVP link is invalid.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _rsvpService.SubmitRSVPAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to submit RSVP.");
                var landing = await _rsvpService.GetPublicLandingAsync(model.EventId);
                if (landing != null)
                {
                    landing.SubmitForm = model;
                    return View("EventDetails", landing);
                }
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to submit RSVP.";
                return RedirectToAction("Index", "Home");
            }

            TempData["SuccessMessage"] = "Thank you! Your RSVP has been confirmed.";
            return RedirectToAction(nameof(Confirmation), new { id = result.RsvpId });
        }

        [HttpGet]
        [Route("RSVP/Confirmation/{id:int}")]
        public async Task<IActionResult> Confirmation(int id)
        {
            var confirmation = await _rsvpService.GetConfirmationAsync(id);
            if (confirmation == null)
            {
                return NotFound();
            }

            return View(confirmation);
        }

        [HttpGet]
        [Route("RSVP/DownloadIcs/{eventId:int}")]
        public async Task<IActionResult> DownloadIcs(int eventId)
        {
            var ev = await _eventRepository.GetByIdAsync(eventId);
            if (ev == null)
            {
                return NotFound();
            }

            var startDateTime = ev.Date.Date.AddHours(9); // Default fallback
            var endDateTime = startDateTime.AddHours(2);   // Default duration
            if (!string.IsNullOrWhiteSpace(ev.Time))
            {
                // Try to parse time range like "09:00 AM - 05:00 PM"
                var timeParts = ev.Time.Split(new[] { "-", "–", "—" }, StringSplitOptions.RemoveEmptyEntries);
                if (timeParts.Length >= 1 && DateTime.TryParse(timeParts[0].Trim(), out var parsedStart))
                {
                    startDateTime = ev.Date.Date.Add(parsedStart.TimeOfDay);
                }
                if (timeParts.Length >= 2 && DateTime.TryParse(timeParts[1].Trim(), out var parsedEnd))
                {
                    endDateTime = ev.Date.Date.Add(parsedEnd.TimeOfDay);
                }
                else if (DateTime.TryParse(ev.Time, out var parsedDt))
                {
                    startDateTime = ev.Date.Date.Add(parsedDt.TimeOfDay);
                }
                else if (TimeSpan.TryParse(ev.Time, out var parsedTs))
                {
                    startDateTime = ev.Date.Date.Add(parsedTs);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//EventEase//Event RSVP System//EN");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{Guid.NewGuid()}@eventease.com");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            // No event time zone is configured; floating local times avoid falsely labeling
            // organizer-entered times as UTC.
            sb.AppendLine($"DTSTART:{startDateTime:yyyyMMdd'T'HHmmss}");
            sb.AppendLine($"DTEND:{endDateTime:yyyyMMdd'T'HHmmss}");
            sb.AppendLine($"SUMMARY:{EscapeCalendarText(ev.Title)}");
            sb.AppendLine($"DESCRIPTION:{EscapeCalendarText(ev.Description ?? ev.Title)}");
            sb.AppendLine($"LOCATION:{EscapeCalendarText(ev.Venue)}");
            sb.AppendLine("END:VEVENT");
            sb.AppendLine("END:VCALENDAR");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/calendar", $"{ev.Title.Replace(" ", "_")}.ics");
        }

        private static string EscapeCalendarText(string value)
        {
            return value.Replace("\\", "\\\\")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n")
                .Replace(";", "\\;")
                .Replace(",", "\\,");
        }
    }
}
