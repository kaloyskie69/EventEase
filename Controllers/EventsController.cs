using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EventEase.Interfaces;
using EventEase.ViewModels.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.CurrentStatus = status ?? "All";
            ViewBag.CurrentSearch = search ?? string.Empty;

            var events = await _eventService.GetEventsByOrganizerAsync(userId, status, search);
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var model = await _eventService.GetEventDetailsAsync(id, userId, baseUrl);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Event not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new EventCreateViewModel
            {
                Date = DateTime.Today.AddDays(7),
                Time = "10:00 AM"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Server-side duplicate event prevention
            if (await _eventService.IsTitleDuplicateAsync(userId, model.Title, model.Date))
            {
                ModelState.AddModelError(nameof(model.Title), "You already have an event with this exact title scheduled on the selected date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var createdEvent = await _eventService.CreateEventAsync(userId, model);
            TempData["SuccessMessage"] = $"Event '{createdEvent.Title}' has been created successfully!";
            return RedirectToAction(nameof(Details), new { id = createdEvent.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _eventService.GetEventForEditAsync(id, userId);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Event not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EventEditViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (await _eventService.IsTitleDuplicateAsync(userId, model.Title, model.Date, model.Id))
            {
                ModelState.AddModelError(nameof(model.Title), "Another event with this title on the selected date already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var success = await _eventService.UpdateEventAsync(userId, model);
            if (!success)
            {
                TempData["ErrorMessage"] = "Unable to update event.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Event updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var success = await _eventService.CancelEventAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "The event has been cancelled.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cancel event.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var success = await _eventService.DeleteEventAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Event deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete event.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var details = await _eventService.GetEventDetailsAsync(id);
            if (details == null)
            {
                return NotFound();
            }

            // Security: Verify the requesting user owns this event before exporting PII
            var eventForOwnership = await _eventService.GetEventForEditAsync(id, userId);
            if (eventForOwnership == null)
            {
                TempData["ErrorMessage"] = "Access denied. You can only export events you organize.";
                return RedirectToAction(nameof(Index));
            }

            var builder = new StringBuilder();
            builder.Append("RSVP ID,Full Name,Email,Phone,RSVP Status,Checked In,Check-In Time,Submitted Date");

            foreach (var cf in details.CustomFields)
            {
                builder.Append($",\"{cf.Label.Replace("\"", "\"\"")}\"");
            }
            builder.AppendLine();

            foreach (var att in details.Attendees)
            {
                builder.Append($"{att.RsvpId},");
                builder.Append($"\"{att.FullName.Replace("\"", "\"\"")}\",");
                builder.Append($"\"{att.Email.Replace("\"", "\"\"")}\",");
                builder.Append($"\"{(att.Phone ?? "").Replace("\"", "\"\"")}\",");
                builder.Append($"\"{att.Status}\",");
                builder.Append(att.CheckedIn ? "Yes," : "No,");
                builder.Append($"\"{att.FormattedCheckInTime}\",");
                builder.Append($"\"{att.SubmittedAt:yyyy-MM-dd HH:mm}\"");

                foreach (var cf in details.CustomFields)
                {
                    var ans = att.CustomAnswers.ContainsKey(cf.Label) ? att.CustomAnswers[cf.Label] : "";
                    builder.Append($",\"{ans.Replace("\"", "\"\"")}\"");
                }
                builder.AppendLine();
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(builder.ToString());
            var bytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);

            var safeTitle = string.Join("_", details.Title.Split(Path.GetInvalidFileNameChars()));
            return File(bytes, "text/csv; charset=utf-8", $"{safeTitle}_Attendance_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
