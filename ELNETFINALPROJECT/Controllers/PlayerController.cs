using Microsoft.AspNetCore.Mvc;

namespace ELNETFINALPROJECT.Controllers
{
    public class PlayerController : Controller
    {
        private const string PlayerSessionKey = "PlayerUsername";
        private readonly AppDbContext _context;

        public PlayerController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString(PlayerSessionKey)))
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return View();
            }

            var account = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Password == password);
            if (account == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View();
            }

            HttpContext.Session.SetString(PlayerSessionKey, account.Username);
            return RedirectToAction("Dashboard");
        }

        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(PlayerSessionKey)))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove(PlayerSessionKey);
            return RedirectToAction("Login");
        }
    }
}