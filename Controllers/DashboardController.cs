using System.Security.Claims;
using System.Threading.Tasks;
using EventEase.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _dashboardService.GetDashboardOverviewAsync(userId);
            model.OrganizerName = User.Identity?.Name ?? "Organizer";

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var model = await _dashboardService.GetDashboardOverviewAsync(userId);
            return Json(new
            {
                monthly = model.MonthlyStats,
                rsvpDistribution = model.RsvpDistribution,
                attendanceByEvent = model.AttendanceByEvent
            });
        }
    }
}
