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

        public sealed class StationActionRequest
        {
            public int StationId { get; set; }
        }

        public sealed class AssignStationRequest
        {
            public int StationId { get; set; }
            public int PlayerId { get; set; }
            public int MinutesPaid { get; set; } = 60;
        }

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

        private void EnsureDefaultStationsExist()
        {
            if (_context.Stations.Any())
                return;

            for (int i = 1; i <= 40; i++)
            {
                _context.Stations.Add(new Station
                {
                    StationNumber = i,
                    Status = "offline",
                    IsPoweredOn = false,
                    IsUnavailable = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.SaveChanges();
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
                var activeSessions = _context.Stations.Count(s => s.Status == "active");
                var availableStations = _context.Stations.Count(s => s.Status == "available");

                var totalRevenue = _context.Accounts
                    .Where(a => a.Role == "Player")
                    .AsEnumerable()
                    .Sum(a => a.Balance);

                var today = DateTime.UtcNow.Date;
                var todayEarnings = _context.Accounts
                    .Where(a => a.Role == "Player" && a.LastLogin.HasValue && a.LastLogin.Value.Date == today)
                    .AsEnumerable()
                    .Sum(a => a.Balance);

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
                    availableStations = availableStations,
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
                EnsureDefaultStationsExist();

                var stations = _context.Stations
                    .OrderBy(s => s.StationNumber)
                    .ToList();

                var playerIds = stations
                    .Where(s => s.CurrentPlayerId.HasValue)
                    .Select(s => s.CurrentPlayerId!.Value)
                    .Distinct()
                    .ToList();

                var playersById = _context.Accounts
                    .Where(a => playerIds.Contains(a.Id))
                    .ToDictionary(a => a.Id);

                var stationStatuses = stations.Select(station =>
                {
                    playersById.TryGetValue(station.CurrentPlayerId ?? -1, out var player);

                    var status = station.IsUnavailable
                        ? "unavailable"
                        : station.Status == "active"
                            ? "active"
                            : station.IsPoweredOn
                                ? "available"
                                : "offline";

                    var elapsedMinutes = station.SessionStartTime.HasValue
                        ? (int)(DateTime.UtcNow - station.SessionStartTime.Value).TotalMinutes
                        : 0;

                    return new
                    {
                        stationId = station.Id,
                        stationNumber = station.StationNumber,
                        status,
                        occupied = status == "active",
                        isPoweredOn = station.IsPoweredOn,
                        isUnavailable = station.IsUnavailable,
                        playerName = player != null ? (player.DisplayName ?? player.Username) : station.CurrentUser,
                        currentGame = player?.CurrentGame,
                        sessionTime = elapsedMinutes > 0 ? $"{elapsedMinutes} min" : null
                    };
                }).ToList();

                return Json(new { success = true, stations = stationStatuses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stations");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Initialize 40 default stations (call once on first setup)
        /// </summary>
        [HttpPost]
        public IActionResult InitializeDefaultStations()
        {
            try
            {
                if (_context.Stations.Any())
                    return Json(new { success = false, message = "Stations already initialized" });

                for (int i = 1; i <= 40; i++)
                {
                    _context.Stations.Add(new Station
                    {
                        StationNumber = i,
                        Status = "offline",
                        IsPoweredOn = false,
                        IsUnavailable = false,
                        CurrentUser = null,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "40 stations initialized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing stations");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get all stations with current status
        /// </summary>
        [HttpGet]
        public IActionResult GetAllStations()
        {
            try
            {
                EnsureDefaultStationsExist();

                var stations = _context.Stations
                    .OrderBy(s => s.StationNumber)
                    .Select(s => new
                    {
                        s.Id,
                        s.StationNumber,
                        s.Status,
                        s.CurrentUser,
                        s.CurrentPlayerId,
                        s.TimeUsedMinutes,
                        s.TimePaidMinutes,
                        s.IsPoweredOn,
                        s.IsUnavailable,
                        s.SessionStartTime
                    })
                    .ToList();

                var stats = new
                {
                    total = stations.Count,
                    active = stations.Count(s => s.Status == "active"),
                    offline = stations.Count(s => s.Status == "offline"),
                    available = stations.Count(s => s.Status == "available"),
                    unavailable = stations.Count(s => s.IsUnavailable)
                };

                return Json(new { success = true, stations, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all stations");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Add a new station
        /// </summary>
        [HttpPost]
        public IActionResult AddStation()
        {
            try
            {
                EnsureDefaultStationsExist();

                var nextStationNumber = (_context.Stations.Max(s => (int?)s.StationNumber) ?? 0) + 1;

                var station = new Station
                {
                    StationNumber = nextStationNumber,
                    Status = "offline",
                    IsPoweredOn = false,
                    IsUnavailable = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Stations.Add(station);
                _context.SaveChanges();

                return Json(new { success = true, station = new { station.Id, station.StationNumber, station.Status } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding station");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove a station permanently
        /// </summary>
        [HttpPost]
        public IActionResult RemoveStation([FromBody] StationActionRequest request)
        {
            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                if (station.CurrentPlayerId.HasValue)
                {
                    var player = _context.Accounts.FirstOrDefault(a => a.Id == station.CurrentPlayerId.Value);
                    if (player != null)
                    {
                        player.CurrentStation = null;
                        player.SessionStartTime = null;
                    }
                }

                _context.Stations.Remove(station);
                _context.SaveChanges();

                return Json(new { success = true, message = "Station removed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing station");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Assign a player to a station (start session)
        /// </summary>
        [HttpPost]
        public IActionResult AssignPlayerToStation([FromBody] AssignStationRequest request)
        {
            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);
                var player = _context.Accounts.FirstOrDefault(a => a.Id == request.PlayerId);

                if (station == null || player == null)
                    return Json(new { success = false, message = "Station or Player not found" });

                station.Status = "active";
                station.CurrentPlayerId = request.PlayerId;
                station.CurrentUser = player.DisplayName ?? player.Username;
                station.TimePaidMinutes = request.MinutesPaid;
                station.TimeUsedMinutes = 0;
                station.IsPoweredOn = true;
                station.SessionStartTime = DateTime.UtcNow;
                station.UpdatedAt = DateTime.UtcNow;

                player.CurrentStation = $"Station {station.StationNumber:D2}";
                player.SessionStartTime = DateTime.UtcNow;

                _context.SaveChanges();

                return Json(new { success = true, message = "Player assigned to station" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning player");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Mark a station as unavailable (hardware problem)
        /// </summary>
        [HttpPost]
        public IActionResult MarkStationUnavailable([FromBody] StationActionRequest request)
        {
            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                station.IsUnavailable = true;
                station.IsPoweredOn = false;
                station.Status = "unavailable";
                station.CurrentPlayerId = null;
                station.CurrentUser = null;
                station.TimePaidMinutes = 0;
                station.TimeUsedMinutes = 0;
                station.UpdatedAt = DateTime.UtcNow;

                _context.SaveChanges();

                return Json(new { success = true, message = "Station marked unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking station unavailable");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Toggle station power on/off
        /// </summary>
        [HttpPost]
        public IActionResult ToggleStationPower([FromBody] StationActionRequest request)
        {
            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                station.IsPoweredOn = !station.IsPoweredOn;

                if (!station.IsPoweredOn)
                {
                    station.Status = "offline";
                    station.CurrentPlayerId = null;
                    station.CurrentUser = null;
                    station.TimePaidMinutes = 0;
                    station.TimeUsedMinutes = 0;
                }
                else if (!station.CurrentPlayerId.HasValue)
                {
                    station.Status = "available";
                }

                station.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();

                return Json(new { success = true, message = "Station power toggled", powered = station.IsPoweredOn });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling station power");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// End session on a station
        /// </summary>
        [HttpPost]
        public IActionResult EndStationSession([FromBody] StationActionRequest request)
        {
            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                if (station.CurrentPlayerId.HasValue)
                {
                    var player = _context.Accounts.FirstOrDefault(a => a.Id == station.CurrentPlayerId.Value);
                    if (player != null)
                    {
                        player.CurrentStation = null;
                        player.SessionStartTime = null;
                    }
                }

                station.Status = "offline";
                station.CurrentPlayerId = null;
                station.CurrentUser = null;
                station.TimePaidMinutes = 0;
                station.TimeUsedMinutes = 0;
                station.IsPoweredOn = false;
                station.SessionStartTime = null;
                station.UpdatedAt = DateTime.UtcNow;

                _context.SaveChanges();

                return Json(new { success = true, message = "Session ended" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session");
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

                player.Password = newPassword;
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

        private int CountPlayersByRank(string rankName)
        {
            return _context.Accounts
                .Where(a => a.Role == "Player")
                .AsEnumerable()
                .Count(a => GetPlayerRank(a) == rankName);
        }

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