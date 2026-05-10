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
            _logger  = logger;
        }

        // ── Helper: check if player is authenticated ───────────────────────────

        private bool IsPlayerAuthenticated()
            => HttpContext.Session.GetInt32("UserId").HasValue;

        private IActionResult RequirePlayer()
        {
            if (!IsPlayerAuthenticated())
                return RedirectToAction("Login", "Player");
            return null!;
        }

        private IActionResult RequirePlayerJson()
        {
            if (!IsPlayerAuthenticated())
                return Json(new { success = false, message = "Not authenticated" });
            return null!;
        }

        // ── Auth ───────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            if (IsPlayerAuthenticated())
                return RedirectToAction("Dashboard");

            return View();
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var identifier = request?.Identifier?.Trim();
            var password   = request?.Password?.Trim();

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
                return BadRequest("Invalid request.");

            var account = _context.Accounts.FirstOrDefault(a =>
                a.Role == "Player" &&
                (a.Email == identifier || a.Username == identifier));

            // FIX: use PasswordHelper.Verify (SHA-256) instead of plain-text comparison
            if (account != null && PasswordHelper.Verify(password, account.Password))
            {
                HttpContext.Session.SetInt32("UserId",    account.Id);
                HttpContext.Session.SetString("UserEmail",  account.Email);
                HttpContext.Session.SetString("UserName",   account.Username);
                HttpContext.Session.SetString("Role",       "Player");

                // FIX: set Status to "Online" on login
                account.LastLogin = DateTime.UtcNow;
                account.Status    = "Online";
                _context.SaveChanges();

                return Ok(new { success = true });
            }

            return Unauthorized("Invalid credentials.");
        }

        public IActionResult Logout()
        {
            // FIX: set Status to "Offline" before clearing session
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId.Value);
                if (account != null)
                {
                    account.Status = "Offline";
                    _context.SaveChanges();
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ── Pages ──────────────────────────────────────────────────────────────

        public IActionResult Dashboard()
        {
            var auth = RequirePlayer();
            if (auth != null) return auth;

            var userId  = HttpContext.Session.GetInt32("UserId")!.Value;
            var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
            if (account == null) return RedirectToAction("Login");

            ViewData["Username"]        = account.Username;
            ViewData["Email"]           = account.Email;
            ViewData["Id"]              = account.Id;
            ViewData["Balance"]         = account.Balance;
            ViewData["DisplayName"]     = account.Username;
            ViewData["IsVerified"]      = account.IsVerified;
            ViewData["PlayerRank"]      = PlayerRankHelper.GetRank(account);

            // Flat rate: ₱20/hr for all stations
            decimal hourlyRate = 20m;
            ViewData["SessionHourlyRate"]   = hourlyRate;
            ViewData["CurrentStation"]      = account.CurrentStation;
            ViewData["RemainingHours"]      = (int)(account.Balance / hourlyRate);
            ViewData["RemainingMinutes"]    = (int)((account.Balance % hourlyRate / hourlyRate) * 60);

            return View();
        }

        public IActionResult Profile()
        {
            var auth = RequirePlayer();
            if (auth != null) return auth;

            return RedirectToAction("Dashboard"); // Profile is embedded inside Dashboard
        }

        // ── API: Profile ───────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult GetProfileData()
        {
            var auth = RequirePlayerJson();
            if (auth != null) return auth;

            try
            {
                var userId  = HttpContext.Session.GetInt32("UserId")!.Value;
                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                    return Json(new { success = false, message = "Account not found" });

                var profilePictureBase64 = account.ProfilePicture != null
                    ? "data:image/png;base64," + Convert.ToBase64String(account.ProfilePicture)
                    : null;

                return Json(new
                {
                    success        = true,
                    username       = account.Username,
                    email          = account.Email,
                    displayName    = account.Username,
                    balance        = account.Balance,
                    isVerified     = account.IsVerified,
                    profilePicture = profilePictureBase64
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                return Json(new { success = false, message = "Error loading profile: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateProfile(IFormFile profilePicture)
        {
            var auth = RequirePlayerJson();
            if (auth != null) return auth;

            try
            {
                var userId  = HttpContext.Session.GetInt32("UserId")!.Value;
                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                    return Json(new { success = false, message = "Account not found" });

                if (profilePicture != null && profilePicture.Length > 0)
                {
                    const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                    if (profilePicture.Length > maxFileSize)
                        return Json(new { success = false, message = "File size exceeds 5 MB limit" });

                    var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    if (!allowedMimeTypes.Contains(profilePicture.ContentType))
                        return Json(new { success = false, message = "Invalid image format" });

                    using var ms = new MemoryStream();
                    profilePicture.CopyTo(ms);
                    account.ProfilePicture = ms.ToArray();
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
            var auth = RequirePlayerJson();
            if (auth != null) return auth;

            try
            {
                if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                    return Json(new { success = false, message = "All fields are required" });

                if (newPassword != confirmPassword)
                    return Json(new { success = false, message = "Passwords do not match" });

                if (newPassword.Length < 6)
                    return Json(new { success = false, message = "Password must be at least 6 characters" });

                var userId  = HttpContext.Session.GetInt32("UserId")!.Value;
                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                    return Json(new { success = false, message = "Account not found" });

                if (!PasswordHelper.Verify(currentPassword, account.Password))
                    return Json(new { success = false, message = "Current password is incorrect" });

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

        // ── API: Balance ───────────────────────────────────────────────────────

        [HttpPost]
        public IActionResult TopUpBalance(decimal amount)
        {
            var auth = RequirePlayerJson();
            if (auth != null) return auth;

            try
            {
                if (amount <= 0)
                    return Json(new { success = false, message = "Amount must be greater than 0" });

                var userId  = HttpContext.Session.GetInt32("UserId")!.Value;
                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                    return Json(new { success = false, message = "Account not found" });

                account.Balance += amount;
                _context.Accounts.Update(account);

                // Record transaction for revenue tracking
                _context.Transactions.Add(new Transaction {
                    Username = account.Username,
                    Type = "TopUp",
                    Amount = amount,
                    StationInfo = account.CurrentStation
                });

                _context.SaveChanges();

                return Json(new { success = true, message = "Top-up successful", newBalance = account.Balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during top-up");
                return Json(new { success = false, message = "Error processing top-up: " + ex.Message });
            }
        }

        // ── API: Dashboard Data ────────────────────────────────────────────────

        /// <summary>Get real dashboard data — player stats, rank, playtime, etc.</summary>
        [HttpGet]
        public IActionResult GetDashboardData()
        {
            var auth = RequirePlayerJson();
            if (auth != null) return auth;

            try
            {
                var userId  = HttpContext.Session.GetInt32("UserId")!.Value;
                var account = _context.Accounts.FirstOrDefault(a => a.Id == userId);
                if (account == null)
                    return Json(new { success = false, message = "Account not found" });

                string playerRank = PlayerRankHelper.GetRank(account);

                var totalPlaytimeHours   = account.TotalPlaytimeMinutes / 60;
                var totalPlaytimeMinutes = account.TotalPlaytimeMinutes % 60;

                // Flat rate: ₱20/hr for all stations
                decimal hourlyRate     = 20m;
                int remainingHours     = (int)(account.Balance / hourlyRate);
                int remainingMinutes   = (int)((account.Balance % hourlyRate / hourlyRate) * 60);

                return Json(new
                {
                    success                  = true,
                    balance                  = account.Balance,
                    playerRank,
                    totalPlaytimeHours,
                    totalPlaytimeMinutes,
                    remainingPlaytimeHours   = remainingHours,
                    remainingPlaytimeMinutes = remainingMinutes,
                    currentStation           = account.CurrentStation,
                    sessionStartTime         = account.SessionStartTime,
                    sessionHourlyRate        = hourlyRate,
                    accountCreatedDate       = account.CreatedAt,
                    lastLogin                = account.LastLogin,
                    totalSessions            = account.TotalSessions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return Json(new { success = false, message = "Error loading dashboard: " + ex.Message });
            }
        }
    }

    public class LoginRequest
    {
        public string Identifier { get; set; } = string.Empty;
        public string Password   { get; set; } = string.Empty;
    }
}