using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ELNETFINALPROJECT.Data;
using ELNETFINALPROJECT.Models;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ELNETFINALPROJECT.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
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

            if (_context.Accounts.Any(a => a.Username == account.Username))
            {
                return BadRequest(new { message = "Username is already taken." });
            }

            if (!string.IsNullOrWhiteSpace(account.Email) && _context.Accounts.Any(a => a.Email == account.Email))
            {
                return BadRequest(new { message = "Email is already registered." });
            }

            account.Role = "Player";
            account.Status = "Offline";
            account.RegisteredDate = DateTime.Now;
            account.CreatedAt = DateTime.UtcNow;

            _context.Accounts.Add(account);
            _context.SaveChanges();

            return Ok(account);
        }

        [HttpGet]
        public IActionResult GetPlayers()
        {
            try
            {
                var players = _context.Accounts
                    .Where(a => a.Role == "Player")
                    .Select(p => new
                    {
                        p.Id,
                        p.Username,
                        p.DisplayName,
                        p.Email,
                        p.Balance,
                        p.Status,
                        p.IsVerified,
                        p.CreatedAt,
                        p.LastLogin,
                        p.TotalSessions,
                        p.TotalPlaytimeMinutes,
                        p.CurrentGame,
                        p.CurrentStation
                    })
                    .ToList();
                
                return Json(new { success = true, players = players });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching players");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get admin dashboard statistics
        /// </summary>
        [HttpGet]
        public IActionResult GetAdminStats()
        {
            try
            {
                var totalPlayers = _context.Accounts.Count(a => a.Role == "Player");
                var onlinePlayers = _context.Accounts.Count(a => a.Role == "Player" && a.Status == "Online");
                var activeSessions = _context.Accounts.Count(a => a.CurrentGame != null && a.SessionStartTime.HasValue);
                
                // Calculate total revenue (all player balances)
                var totalRevenue = _context.Accounts.Where(a => a.Role == "Player").Sum(a => a.Balance);
                
                // Calculate today's earnings
                var today = DateTime.UtcNow.Date;
                var todayEarnings = _context.Accounts
                    .Where(a => a.Role == "Player" && a.LastLogin.HasValue && a.LastLogin.Value.Date == today)
                    .Sum(a => a.Balance);

                // Get player rank distribution
                var rankDistribution = new
                {
                    Legend = CountPlayersByRank("Legend"),
                    Diamond = CountPlayersByRank("Diamond"),
                    Platinum = CountPlayersByRank("Platinum"),
                    Gold = CountPlayersByRank("Gold"),
                    Silver = CountPlayersByRank("Silver"),
                    Bronze = CountPlayersByRank("Bronze")
                };

                return Json(new
                {
                    success = true,
                    totalPlayers = totalPlayers,
                    onlinePlayers = onlinePlayers,
                    activeSessions = activeSessions,
                    totalRevenue = totalRevenue,
                    todayEarnings = todayEarnings,
                    availableStations = 42, // Static for now, can be dynamic
                    rankDistribution = rankDistribution
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get live gaming stations status
        /// </summary>
        [HttpGet]
        public IActionResult GetStationsStatus()
        {
            try
            {
                var stations = new List<object>();
                
                // Get all unique stations from active sessions
                var activeSessions = _context.Accounts
                    .Where(a => a.CurrentStation != null && a.SessionStartTime.HasValue)
                    .GroupBy(a => a.CurrentStation)
                    .ToList();

                // Create station list (1-42)
                for (int i = 1; i <= 42; i++)
                {
                    var stationNum = $"Station {i:D2}";
                    var activeSession = activeSessions.FirstOrDefault(s => s.Key == stationNum);
                    
                    if (activeSession != null)
                    {
                        var player = activeSession.First();
                        stations.Add(new
                        {
                            stationId = i,
                            stationName = stationNum,
                            status = "occupied",
                            playerName = player.DisplayName ?? player.Username,
                            currentGame = player.CurrentGame,
                            elapsedMinutes = (int)(DateTime.UtcNow - (player.SessionStartTime ?? DateTime.UtcNow)).TotalMinutes
                        });
                    }
                    else
                    {
                        stations.Add(new
                        {
                            stationId = i,
                            stationName = stationNum,
                            status = "available",
                            playerName = (string)null,
                            currentGame = (string)null,
                            elapsedMinutes = 0
                        });
                    }
                }

                return Json(new { success = true, stations = stations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stations");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get player activity log for today
        /// </summary>
        [HttpGet]
        public IActionResult GetActivityLog(int limit = 10)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                
                var activities = _context.Accounts
                    .Where(a => a.Role == "Player" && a.LastLogin.HasValue && a.LastLogin.Value.Date == today)
                    .OrderByDescending(a => a.LastLogin)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        username = a.DisplayName ?? a.Username,
                        action = "Login",
                        timestamp = a.LastLogin,
                        game = a.CurrentGame ?? "None",
                        duration = (DateTime.UtcNow - (a.SessionStartTime ?? a.LastLogin.Value)).TotalMinutes
                    })
                    .ToList();

                return Json(new { success = true, activities = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity log");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a player (admin action)
        /// </summary>
        [HttpDelete]
        public IActionResult DeletePlayer(int playerId)
        {
            try
            {
                var player = _context.Accounts.FirstOrDefault(a => a.Id == playerId);
                if (player == null)
                    return Json(new { success = false, message = "Player not found" });

                _context.Accounts.Remove(player);
                _context.SaveChanges();

                return Json(new { success = true, message = "Player deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting player");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update player balance (admin action)
        /// </summary>
        [HttpPost]
        public IActionResult UpdatePlayerBalance(int playerId, decimal amount)
        {
            try
            {
                var player = _context.Accounts.FirstOrDefault(a => a.Id == playerId);
                if (player == null)
                    return Json(new { success = false, message = "Player not found" });

                player.Balance = amount;
                _context.Accounts.Update(player);
                _context.SaveChanges();

                return Json(new { success = true, newBalance = player.Balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating balance");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Reset player password (admin action)
        /// </summary>
        [HttpPost]
        public IActionResult ResetPlayerPassword(int playerId, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                    return Json(new { success = false, message = "Password cannot be empty" });

                var player = _context.Accounts.FirstOrDefault(a => a.Id == playerId);
                if (player == null)
                    return Json(new { success = false, message = "Player not found" });

                player.Password = newPassword; // In production, hash this!
                _context.Accounts.Update(player);
                _context.SaveChanges();

                return Json(new { success = true, message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Count players by rank
        /// </summary>
        private int CountPlayersByRank(string rankName)
        {
            return _context.Accounts
                .Where(a => a.Role == "Player")
                .AsEnumerable()
                .Count(a => GetPlayerRank(a) == rankName);
        }

        /// <summary>
        /// Calculate player rank based on account metrics
        /// </summary>
        private string GetPlayerRank(Account account)
        {
            var accountAgeInDays = (DateTime.UtcNow - account.CreatedAt).TotalDays;
            
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

        public IActionResult Stations()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}