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
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Invalid request.");
            }

            var account = _context.Accounts.FirstOrDefault(a => a.Email == request.Email && a.Password == request.Password && a.Role == "Player");

            if (account != null)
            {
                HttpContext.Session.SetString("UserEmail", account.Email);
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
            return View(); // Returns Views/Player/Dashboard.cshtml
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
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
