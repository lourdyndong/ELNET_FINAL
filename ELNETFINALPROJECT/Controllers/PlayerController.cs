using Microsoft.AspNetCore.Mvc;
using ELNETFINALPROJECT.Data;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ELNETFINALPROJECT.Controllers
{
    public class PlayerController : Controller
    {
        private readonly AppDbContext _context;

        public PlayerController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(); // Returns Views/Player/Login.cshtml
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var identifier = request?.Identifier?.Trim();
            var password = request?.Password?.Trim();

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                return BadRequest("Invalid request.");
            }

            var account = _context.Accounts.FirstOrDefault(a =>
                a.Role == "Player" &&
                (a.Email == identifier || a.Username == identifier) &&
                a.Password == password);

            if (account != null)
            {
                HttpContext.Session.SetString("UserEmail", account.Email);
                HttpContext.Session.SetString("UserName", account.Username);
                HttpContext.Session.SetString("Role", "Player");
                return Ok(new { success = true });
            }

            return Unauthorized("Invalid credentials.");
        }

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Player")
            {
                return RedirectToAction("Login");
            }

            var identifier = HttpContext.Session.GetString("UserEmail") ?? HttpContext.Session.GetString("UserName");
            var account = _context.Accounts.FirstOrDefault(a => a.Role == "Player" && (a.Email == identifier || a.Username == identifier));

            ViewData["Username"] = account?.Username ?? HttpContext.Session.GetString("UserName") ?? "Player";
            ViewData["Email"] = account?.Email ?? HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            ViewData["Id"] = account?.Id.ToString() ?? string.Empty;
            ViewData["Balance"] = account?.Balance ?? 0m;

            return View(); // Returns Views/Player/Dashboard.cshtml
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("Role") != "Player")
            {
                return RedirectToAction("Login");
            }
            return View(); // Returns Views/Player/Profile.cshtml
        }
    }

    public class LoginRequest
    {
        public string Identifier { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}