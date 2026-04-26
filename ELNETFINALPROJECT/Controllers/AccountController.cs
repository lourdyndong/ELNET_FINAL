using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ELNETFINALPROJECT.Data;
using ELNETFINALPROJECT.Helpers;
using ELNETFINALPROJECT.Models;

namespace ELNETFINALPROJECT.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usernameOrId, string password, string? rememberMe)
        {
            if (string.IsNullOrWhiteSpace(usernameOrId) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Please provide credentials.";
                return RedirectToAction("Index", "Home");
            }

            // Determine if student id (8 digits)
            User? user = null;
            if (usernameOrId.All(char.IsDigit) && usernameOrId.Length == 8)
            {
                user = _db.Users.FirstOrDefault(u => u.StudentId == usernameOrId);
            }
            else
            {
                user = _db.Users.FirstOrDefault(u => u.Username == usernameOrId);
            }

            if (user == null || !PasswordHelper.Verify(password, user.PasswordHash))
            {
                TempData["Error"] = "Invalid credentials.";
                return RedirectToAction("Index", "Home");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            };

            if (!string.IsNullOrEmpty(user.StudentId))
            {
                claims.Add(new Claim("StudentId", user.StudentId));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = !string.IsNullOrEmpty(rememberMe)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // Redirect based on role
            if (user.Role == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Dashboard", "Student");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string studentId, string fullName, string password, string confirmPassword, string? agreeTerms)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Please fill required fields.";
                return RedirectToAction("Index", "Home");
            }

            if (!studentId.All(char.IsDigit) || studentId.Length != 8)
            {
                TempData["Error"] = "Student ID must be exactly 8 digits.";
                return RedirectToAction("Index", "Home");
            }

            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("Index", "Home");
            }

            if (_db.Users.Any(u => u.StudentId == studentId))
            {
                TempData["Error"] = "Student ID already registered.";
                return RedirectToAction("Index", "Home");
            }

            var user = new User
            {
                StudentId = studentId,
                FullName = fullName,
                PasswordHash = PasswordHelper.Hash(password),
                Role = "Student"
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            TempData["Message"] = "Registration successful. You can now login.";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
