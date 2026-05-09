using Microsoft.AspNetCore.Mvc;
using ELNETFINALPROJECT.Data;
using ELNETFINALPROJECT.Models;
using ELNETFINALPROJECT.Helpers;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ELNETFINALPROJECT.Controllers
{
    public class PlayerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(AppDbContext context, ILogger<PlayerController> logger)
        {
            _context = context;
            _logger = logger;
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
                HttpContext.Session.SetInt32("UserId", account.Id);
                HttpContext.Session.SetString("UserEmail", account.Email);
                HttpContext.Session.SetString("UserName", account.Username);
                HttpContext.Session.SetString("Role", "Player");
                account.LastLogin = DateTime.UtcNow;
                _context.SaveChanges();
                return Ok(new { success = true });
            }

            return Unauthorized("Invalid credentials.");
        }

        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Player");
            }

            var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
            if (account == null)
            {
                return RedirectToAction("Login", "Player");
            }

            ViewData["Username"] = account.Username;
            ViewData["Email"] = account.Email;
            ViewData["Id"] = account.Id;
            ViewData["Balance"] = account.Balance;
            ViewData["DisplayName"] = account.DisplayName ?? account.Username;
            ViewData["IsVerified"] = account.IsVerified;

            return View();
        }

        [HttpPost]
        public IActionResult UpdateProfile(IFormFile profilePicture)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                {
                    return Json(new { success = false, message = "Account not found" });
                }

                if (profilePicture != null && profilePicture.Length > 0)
                {
                    const long maxFileSize = 5 * 1024 * 1024; // 5MB
                    if (profilePicture.Length > maxFileSize)
                    {
                        return Json(new { success = false, message = "File size exceeds 5MB limit" });
                    }

                    var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    if (!allowedMimeTypes.Contains(profilePicture.ContentType))
                    {
                        return Json(new { success = false, message = "Invalid image format" });
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        profilePicture.CopyTo(memoryStream);
                        account.ProfilePicture = memoryStream.ToArray();
                    }
                }

                _context.Accounts.Update(account);
                _context.SaveChanges();

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return Json(new { success = false, message = "Error updating profile: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateCredentials(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Passwords do not match" });
                }

                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Password must be at least 6 characters" });
                }

                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                {
                    return Json(new { success = false, message = "Account not found" });
                }

                // Verify current password
                if (!PasswordHelper.Verify(currentPassword, account.Password))
                {
                    return Json(new { success = false, message = "Current password is incorrect" });
                }

                // Update password
                account.Password = PasswordHelper.Hash(newPassword);

                _context.Accounts.Update(account);
                _context.SaveChanges();

                return Json(new { success = true, message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credentials");
                return Json(new { success = false, message = "Error updating credentials: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult TopUpBalance(decimal amount)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                if (amount <= 0)
                {
                    return Json(new { success = false, message = "Amount must be greater than 0" });
                }

                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                {
                    return Json(new { success = false, message = "Account not found" });
                }

                account.Balance += amount;

                _context.Accounts.Update(account);
                _context.SaveChanges();

                return Json(new { success = true, message = "Top-up successful", newBalance = account.Balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during top-up");
                return Json(new { success = false, message = "Error processing top-up: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetProfileData()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                {
                    return Json(new { success = false, message = "Account not found" });
                }

                var profilePictureBase64 = account.ProfilePicture != null 
                    ? "data:image/png;base64," + Convert.ToBase64String(account.ProfilePicture)
                    : null;

                return Json(new 
                { 
                    success = true, 
                    username = account.Username,
                    email = account.Email,
                    displayName = account.DisplayName ?? account.Username,
                    balance = account.Balance,
                    isVerified = account.IsVerified,
                    profilePicture = profilePictureBase64
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                return Json(new { success = false, message = "Error loading profile: " + ex.Message });
            }
        }

        /// <summary>
        /// Get real dashboard data - player stats, rank, playtime, etc.
        /// </summary>
        [HttpGet]
        public IActionResult GetDashboardData()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return Json(new { success = false, message = "Not authenticated" });
                }

                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                {
                    return Json(new { success = false, message = "Account not found" });
                }

                // Calculate player rank based on account age and activity
                string playerRank = GetPlayerRank(account);

                // Calculate total playtime (hours and minutes)
                var totalPlaytimeHours = account.TotalPlaytimeMinutes / 60;
                var totalPlaytimeMinutes = account.TotalPlaytimeMinutes % 60;

                // Calculate remaining playtime from balance (assuming ₱25/hour rate)
                decimal hourlyRate = 25m;
                int remainingHours = (int)(account.Balance / hourlyRate);
                int remainingMinutes = (int)((account.Balance % hourlyRate / hourlyRate) * 60);

                return Json(new
                {
                    success = true,
                    balance = account.Balance,
                    playerRank = playerRank,
                    totalPlaytimeHours = totalPlaytimeHours,
                    totalPlaytimeMinutes = totalPlaytimeMinutes,
                    remainingPlaytimeHours = remainingHours,
                    remainingPlaytimeMinutes = remainingMinutes,
                    accountCreatedDate = account.CreatedAt,
                    lastLogin = account.LastLogin,
                    totalSessions = account.TotalSessions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return Json(new { success = false, message = "Error loading dashboard: " + ex.Message });
            }
        }

        /// <summary>
        /// Determine player rank based on account metrics
        /// </summary>
        private string GetPlayerRank(Account account)
        {
            var accountAgeInDays = (DateTime.UtcNow - account.CreatedAt).TotalDays;
            
            // Rank progression: Bronze -> Silver -> Gold -> Platinum -> Diamond -> Legend
            if (account.TotalSessions >= 100 && accountAgeInDays >= 90)
                return "Legend";
            if (account.TotalSessions >= 75 && accountAgeInDays >= 60)
                return "Diamond";
            if (account.TotalSessions >= 50 && accountAgeInDays >= 30)
                return "Platinum";
            if (account.TotalSessions >= 25 && accountAgeInDays >= 14)
                return "Gold";
            if (account.TotalSessions >= 10 && accountAgeInDays >= 7)
                return "Silver";
            
            return "Bronze";
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }

    public class LoginRequest
    {
        public string Identifier { get; set; }
        public string Password { get; set; }
    }
}