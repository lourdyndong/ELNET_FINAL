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

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Index");
            }
            return View();
        }
    }
}
