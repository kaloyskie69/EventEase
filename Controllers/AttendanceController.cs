using System.Threading.Tasks;
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

            var roster = await _attendanceService.GetCheckInRosterAsync(eventId, search, filter);
            if (roster == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Index", "Events");
            }

            return View(roster);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // Enabled for seamless AJAX check-in calls with JSON payloads
        public async Task<IActionResult> ToggleCheckIn([FromBody] CheckInToggleRequest request)
        {
            if (request == null || request.RsvpId <= 0)
            {
                return Json(new { success = false, message = "Invalid request payload." });
            }

            var result = await _attendanceService.ToggleCheckInAsync(request.RsvpId, request.Undo);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCheckIn(int rsvpId, int eventId)
        {
            var result = await _attendanceService.CheckInAttendeeAsync(rsvpId);
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
            var result = await _attendanceService.UndoCheckInAttendeeAsync(rsvpId);
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
