using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = System.DateTime.Today;
            var upcomingEvents = await _context.Events
                .Where(e => e.Status == "Upcoming" && e.Date >= today)
                .OrderBy(e => e.Date)
                .Take(3)
                .ToListAsync();

            return View(upcomingEvents);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
