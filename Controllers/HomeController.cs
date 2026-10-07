using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EventEase.Data;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoDbContext _context;

        public HomeController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = System.DateTime.Today;
            var events = await _context.Events.FindAsync(e => e.Status == "Upcoming");
            var upcomingEvents = events
                .Where(e => e.Date.Date >= today)
                .OrderBy(e => e.Date)
                .Take(3)
                .ToList();

            return View(upcomingEvents);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
