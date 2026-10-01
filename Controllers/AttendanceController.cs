using System.Threading.Tasks;
using System.Security.Claims;
using EventEase.Interfaces;
using EventEase.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet]
        public async Task<IActionResult> CheckIn(int eventId, string? search, string? filter)
        {
            ViewBag.CurrentSearch = search ?? string.Empty;
            ViewBag.CurrentFilter = filter ?? "All";

            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerId)) return Challenge();

            var roster = await _attendanceService.GetCheckInRosterAsync(eventId, organizerId, search, filter);
            if (roster == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Index", "Events");
            }

            return View(roster);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCheckIn([FromBody] CheckInToggleRequest request)
        {
            if (request == null || request.RsvpId <= 0)
            {
                return Json(new { success = false, message = "Invalid request payload." });
            }

            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerId)) return Unauthorized();

            var result = await _attendanceService.ToggleCheckInAsync(request.RsvpId, organizerId, request.Undo);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCheckIn(int rsvpId, int eventId)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerId)) return Challenge();
            var result = await _attendanceService.CheckInAttendeeAsync(rsvpId, organizerId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(CheckIn), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoCheckIn(int rsvpId, int eventId)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerId)) return Challenge();
            var result = await _attendanceService.UndoCheckInAttendeeAsync(rsvpId, organizerId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(CheckIn), new { eventId });
        }
    }
}
