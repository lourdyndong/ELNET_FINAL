using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ELNETFINALPROJECT.Data;
using ELNETFINALPROJECT.Models;

namespace ELNETFINALPROJECT.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string password)
        {
            if (password == "admin123")
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Dashboard");
            }
            TempData["Error"] = "Invalid password!";
            return RedirectToAction("Index");
        }

        private IActionResult RequireAdmin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Index");
            return null;
        }

        public IActionResult Dashboard()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Players()
        {
            return RequireAdmin() ?? View();
        }

        [HttpPost]
        public IActionResult RegisterPlayer([FromBody] Account account)
        {
            if (string.IsNullOrWhiteSpace(account.Username) || string.IsNullOrWhiteSpace(account.Password))
            {
                return BadRequest(new { message = "Username and Password are required." });
            }

            account.Role = "Player";
            account.Status = "Offline";
            account.RegisteredDate = DateTime.Now;

            _context.Accounts.Add(account);
            _context.SaveChanges();

            return Ok(account);
        }

        [HttpGet]
        public IActionResult GetPlayers()
        {
            var players = _context.Accounts.Where(a => a.Role == "Player").ToList();
            return Json(players);
        }

        public IActionResult Stations()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Games()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Settings()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Support()
        {
            return RequireAdmin() ?? View();
        }
    }
}