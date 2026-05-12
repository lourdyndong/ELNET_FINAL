using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ELNETFINALPROJECT.Data;
using ELNETFINALPROJECT.Models;
using ELNETFINALPROJECT.Helpers;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ELNETFINALPROJECT.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

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

        public sealed class AssignGuestRequest
        {
            public int StationId { get; set; }
            public string GuestName { get; set; } = string.Empty;
            public int MinutesPaid { get; set; } = 60;
        }

        public class ReassignStationRequest
        {
            public int OldStationId { get; set; }
            public int NewStationId { get; set; }
        }

        public HomeController(AppDbContext context, ILogger<HomeController> logger, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
        }


        private bool IsAdminAuthenticated()
            => HttpContext.Session.GetString("IsAdmin") == "true";

        private IActionResult RequireAdmin()
        {
            if (!IsAdminAuthenticated())
                return RedirectToAction("Index");
            return null!;
        }

        private IActionResult RequireAdminJson()
        {
            if (!IsAdminAuthenticated())
                return Unauthorized(new { message = "Unauthorized. Please log in as admin." });
            return null!;
        }

        // ── Pages ──────────────────────────────────────────────────────────────

        public IActionResult Index()
        {
            return View();
        }

        // FIX: read password from config; protect with anti-forgery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string password)
        {
            var adminPassword = _config["AdminPassword"] ?? "admin123";
            if (password == adminPassword)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Dashboard");
            }
            TempData["Error"] = "Invalid password!";
            return RedirectToAction("Index");
        }

        public IActionResult Dashboard()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Players()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Stations()
        {
            return RequireAdmin() ?? View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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

        private int CountPlayersByRank(string rankName)
        {
            return _context.Accounts
                .Where(a => a.Role == "Player")
                .AsEnumerable()
                .Count(a => PlayerRankHelper.GetRank(a) == rankName);
        }

        // ── Player Registration ────────────────────────────────────────────────

        /// <summary>Register a new player account (admin-only action).</summary>
        [HttpPost]
        public IActionResult RegisterPlayer([FromBody] Account account)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            // Server-side validation using ModelState (per course Validation PDF)
            if (string.IsNullOrWhiteSpace(account.Username) || string.IsNullOrWhiteSpace(account.Password))
                return BadRequest(new { message = "Username and Password are required." });

            if (account.Password.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters." });

            if (!string.IsNullOrWhiteSpace(account.Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(account.Email))
                return BadRequest(new { message = "Invalid email format." });

            if (_context.Accounts.Any(a => a.Username == account.Username))
                return BadRequest(new { message = "Username is already taken." });

            if (!string.IsNullOrWhiteSpace(account.Email) && _context.Accounts.Any(a => a.Email == account.Email))
                return BadRequest(new { message = "Email is already registered." });

            // Hash password before saving (security best practice)
            account.Password = PasswordHelper.Hash(account.Password);
            account.Role = "Player";
            account.Status = "Offline";
            account.RegisteredDate = DateTime.Now;
            account.CreatedAt = DateTime.UtcNow;
            account.SessionHourlyRate = 20m;

            _context.Accounts.Add(account);
            _context.SaveChanges();

            // Record transaction if initial balance was set
            if (account.Balance > 0)
            {
                _context.Transactions.Add(new Transaction {
                    Username = account.Username,
                    Type = "Registration",
                    Amount = account.Balance,
                    StationInfo = "Initial Balance"
                });
                _context.SaveChanges();
            }

            return Ok(account);
        }

        // ── Player Queries ─────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult GetPlayers()
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

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

                return Json(new { success = true, players });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching players");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Get admin dashboard statistics.</summary>
        [HttpGet]
        public IActionResult GetAdminStats()
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var totalPlayers    = _context.Accounts.Count(a => a.Role == "Player");
                var onlinePlayers   = _context.Accounts.Count(a => a.Role == "Player" && a.Status == "Online");
                var activeSessions  = _context.Stations.Count(s => s.Status == "active");
                var availableStations = _context.Stations.Count(s => s.Status == "available");
                var totalStations = _context.Stations.Count();

                // Revenue: Total money collected (from Transactions table)
                // Note: SQLite does not support Sum on decimal; use AsEnumerable for client-side aggregation
                var totalRevenue = _context.Transactions
                    .AsEnumerable()
                    .Sum(t => t.Amount);

                // Today's Earnings: Money collected from top-ups and guests today
                var today = DateTime.UtcNow.Date;
                var todayEarnings = _context.Transactions
                    .AsEnumerable()
                    .Where(t => t.CreatedAt.Date == today)
                    .Sum(t => t.Amount);

                var rankDistribution = new
                {
                    Legend   = CountPlayersByRank("Legend"),
                    Diamond  = CountPlayersByRank("Diamond"),
                    Platinum = CountPlayersByRank("Platinum"),
                    Gold     = CountPlayersByRank("Gold"),
                    Silver   = CountPlayersByRank("Silver"),
                    Bronze   = CountPlayersByRank("Bronze")
                };

                return Json(new
                {
                    success = true,
                    totalPlayers,
                    onlinePlayers,
                    activeSessions,
                    totalRevenue,
                    todayEarnings,
                    availableStations,
                    totalStations,
                    rankDistribution
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Get live gaming stations status.</summary>
        [HttpGet]
        public IActionResult GetStationsStatus()
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

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
                        stationId      = station.Id,
                        stationNumber  = station.StationNumber,
                        status,
                        occupied       = status == "active",
                        isPoweredOn    = station.IsPoweredOn,
                        isUnavailable  = station.IsUnavailable,
                        playerName     = player != null ? player.Username : station.CurrentUser,
                        playerId       = station.CurrentPlayerId,
                        currentGame    = player?.CurrentGame,
                        sessionTime    = elapsedMinutes > 0 ? $"{elapsedMinutes} min" : null,
                        timePaid       = station.TimePaidMinutes,
                        timeUsed       = elapsedMinutes
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

        /// <summary>Initialize 40 default stations (call once on first setup).</summary>
        [HttpPost]
        public IActionResult InitializeDefaultStations()
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                if (_context.Stations.Any())
                    return Json(new { success = false, message = "Stations already initialized" });

                for (int i = 1; i <= 40; i++)
                {
                    _context.Stations.Add(new Station
                    {
                        StationNumber = i,
                        Status        = "offline",
                        IsPoweredOn   = false,
                        IsUnavailable = false,
                        CurrentUser   = null,
                        CreatedAt     = DateTime.UtcNow
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

        /// <summary>Get all stations with current status.</summary>
        [HttpGet]
        public IActionResult GetAllStations()
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                EnsureDefaultStationsExist();

                var stationsDb = _context.Stations.OrderBy(s => s.StationNumber).ToList();
                var activePlayerIds = stationsDb.Where(s => s.CurrentPlayerId.HasValue).Select(s => s.CurrentPlayerId.Value).ToList();
                var players = _context.Accounts.Where(a => activePlayerIds.Contains(a.Id)).ToDictionary(a => a.Id);

                var stations = new List<object>();
                foreach (var s in stationsDb)
                {
                    int timePaid = s.TimePaidMinutes;
                    if (s.CurrentPlayerId.HasValue && players.TryGetValue(s.CurrentPlayerId.Value, out var player))
                    {
                        timePaid = (int)((player.Balance / 20m) * 60m) + s.TimeUsedMinutes;
                    }
                    stations.Add(new
                    {
                        s.Id,
                        s.StationNumber,
                        s.Status,
                        s.CurrentUser,
                        s.CurrentPlayerId,
                        s.TimeUsedMinutes,
                        TimePaidMinutes = timePaid,
                        s.IsPoweredOn,
                        s.IsUnavailable,
                        s.SessionStartTime
                    });
                }

                var stats = new
                {
                    total       = stationsDb.Count,
                    active      = stationsDb.Count(s => s.Status == "active"),
                    offline     = stationsDb.Count(s => s.Status == "offline"),
                    available   = stationsDb.Count(s => s.Status == "available"),
                    unavailable = stationsDb.Count(s => s.IsUnavailable)
                };

                return Json(new { success = true, stations, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all stations");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Station Management ─────────────────────────────────────────────────

        /// <summary>Add a new station.</summary>
        [HttpPost]
        public IActionResult AddStation()
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                EnsureDefaultStationsExist();

                var nextStationNumber = (_context.Stations.Max(s => (int?)s.StationNumber) ?? 0) + 1;

                var station = new Station
                {
                    StationNumber = nextStationNumber,
                    Status        = "offline",
                    IsPoweredOn   = false,
                    IsUnavailable = false,
                    CreatedAt     = DateTime.UtcNow
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

        /// <summary>Remove a station permanently.</summary>
        [HttpPost]
        public IActionResult RemoveStation([FromBody] StationActionRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

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
                        player.Status = "Offline";
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

        /// <summary>Assign a registered player to a station (start session).</summary>
        [HttpPost]
        public IActionResult AssignPlayerToStation([FromBody] AssignStationRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);
                var player  = _context.Accounts.FirstOrDefault(a => a.Id == request.PlayerId);

                if (station == null || player == null)
                    return Json(new { success = false, message = "Station or Player not found" });

                station.Status          = "active";
                station.CurrentPlayerId = request.PlayerId;
                station.CurrentUser     = player.Username;
                
                // If it's a registered player, calculate TimePaid based on their current balance
                station.TimePaidMinutes = (int)((player.Balance / 20m) * 60m);
                station.TimeUsedMinutes = 0;
                station.IsPoweredOn     = true;
                station.SessionStartTime = DateTime.UtcNow;
                station.UpdatedAt       = DateTime.UtcNow;

                player.CurrentStation   = $"Station {station.StationNumber:D2}";
                player.SessionStartTime = DateTime.UtcNow;
                player.Status           = "Online";

                _context.SaveChanges();

                return Json(new { success = true, message = "Player assigned to station" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning player");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Assign a GUEST (no account) to a station.</summary>
        [HttpPost]
        public IActionResult AssignGuestToStation([FromBody] AssignGuestRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                if (string.IsNullOrWhiteSpace(request.GuestName))
                    return Json(new { success = false, message = "Guest name is required" });

                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                station.Status           = "active";
                station.CurrentPlayerId  = null;
                station.CurrentUser      = request.GuestName.Trim();
                station.TimePaidMinutes  = request.MinutesPaid;
                station.TimeUsedMinutes  = 0;
                station.IsPoweredOn      = true;
                station.SessionStartTime = DateTime.UtcNow;
                station.UpdatedAt        = DateTime.UtcNow;

                // Record transaction for guest payment
                decimal guestPayment = (request.MinutesPaid / 60m) * 20m;
                _context.Transactions.Add(new Transaction {
                    Username = station.CurrentUser,
                    Type = "Guest",
                    Amount = guestPayment,
                    StationInfo = $"PC {station.StationNumber}"
                });

                _context.SaveChanges();

                return Json(new { success = true, message = $"Guest '{request.GuestName}' assigned to station" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning guest to station");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Toggle a station as unavailable/available.</summary>
        [HttpPost]
        public IActionResult ToggleStationUnavailable([FromBody] StationActionRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                if (station.IsUnavailable)
                {
                    // Mark as available
                    station.IsUnavailable = false;
                    station.Status = station.IsPoweredOn ? "available" : "offline";
                    station.UpdatedAt = DateTime.UtcNow;
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Station marked available", isUnavailable = false });
                }

                // Mark as unavailable
                // Clear any active player session
                if (station.CurrentPlayerId.HasValue)
                {
                    var player = _context.Accounts.FirstOrDefault(a => a.Id == station.CurrentPlayerId.Value);
                    if (player != null)
                    {
                        player.CurrentStation   = null;
                        player.SessionStartTime = null;
                        player.Status           = "Offline";
                    }
                }

                station.IsUnavailable   = true;
                station.IsPoweredOn     = false;
                station.Status          = "unavailable";
                station.CurrentPlayerId = null;
                station.CurrentUser     = null;
                station.TimePaidMinutes = 0;
                station.TimeUsedMinutes = 0;
                station.UpdatedAt       = DateTime.UtcNow;

                _context.SaveChanges();

                return Json(new { success = true, message = "Station marked unavailable", isUnavailable = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling station unavailable");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Reassign a player to a different station.</summary>
        [HttpPost]
        public IActionResult ReassignStation([FromBody] ReassignStationRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var oldStation = _context.Stations.FirstOrDefault(s => s.Id == request.OldStationId);
                var newStation = _context.Stations.FirstOrDefault(s => s.Id == request.NewStationId);

                if (oldStation == null || newStation == null)
                    return Json(new { success = false, message = "Station not found" });

                if (oldStation.Status != "active")
                    return Json(new { success = false, message = "Old station is not active" });

                if (newStation.Status == "active")
                    return Json(new { success = false, message = "New station is already occupied" });
                
                if (newStation.IsUnavailable)
                    return Json(new { success = false, message = "New station is unavailable" });

                // Transfer data
                newStation.Status = "active";
                newStation.CurrentPlayerId = oldStation.CurrentPlayerId;
                newStation.CurrentUser = oldStation.CurrentUser;
                newStation.TimePaidMinutes = oldStation.TimePaidMinutes;
                newStation.TimeUsedMinutes = oldStation.TimeUsedMinutes;
                newStation.IsPoweredOn = true;
                newStation.SessionStartTime = oldStation.SessionStartTime;
                newStation.UpdatedAt = DateTime.UtcNow;

                // Clear old station
                oldStation.Status = "available";
                oldStation.CurrentPlayerId = null;
                oldStation.CurrentUser = null;
                oldStation.TimePaidMinutes = 0;
                oldStation.TimeUsedMinutes = 0;
                oldStation.IsPoweredOn = true;
                oldStation.SessionStartTime = null;
                oldStation.UpdatedAt = DateTime.UtcNow;

                // Update player's current station
                if (newStation.CurrentPlayerId.HasValue)
                {
                    var player = _context.Accounts.FirstOrDefault(a => a.Id == newStation.CurrentPlayerId.Value);
                    if (player != null)
                    {
                        player.CurrentStation = $"Station {newStation.StationNumber:D2}";
                    }
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Station reassigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning station");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Toggle station power on/off.</summary>
        [HttpPost]
        public IActionResult ToggleStationPower([FromBody] StationActionRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                station.IsPoweredOn = !station.IsPoweredOn;

                if (!station.IsPoweredOn)
                {
                    // Power off — clear any session
                    if (station.CurrentPlayerId.HasValue)
                    {
                        var player = _context.Accounts.FirstOrDefault(a => a.Id == station.CurrentPlayerId.Value);
                        if (player != null)
                        {
                            player.CurrentStation   = null;
                            player.SessionStartTime = null;
                            player.Status           = "Offline";
                        }
                    }

                    station.Status          = "offline";
                    station.CurrentPlayerId = null;
                    station.CurrentUser     = null;
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

        /// <summary>End session on a station.</summary>
        [HttpPost]
        public IActionResult EndStationSession([FromBody] StationActionRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var station = _context.Stations.FirstOrDefault(s => s.Id == request.StationId);

                if (station == null)
                    return Json(new { success = false, message = "Station not found" });

                // FIX: update player stats before ending the session
                if (station.CurrentPlayerId.HasValue)
                {
                    var player = _context.Accounts.FirstOrDefault(a => a.Id == station.CurrentPlayerId.Value);
                    if (player != null)
                    {
                        // Calculate actual minutes played
                        var minutesPlayed = station.SessionStartTime.HasValue
                            ? (int)(DateTime.UtcNow - station.SessionStartTime.Value).TotalMinutes
                            : station.TimeUsedMinutes;

                        player.TotalSessions++;
                        player.TotalPlaytimeMinutes += minutesPlayed;
                        player.CurrentStation        = null;
                        player.SessionStartTime      = null;
                        player.Status                = "Offline";
                    }
                }

                station.Status           = "offline";
                station.CurrentPlayerId  = null;
                station.CurrentUser      = null;
                station.TimePaidMinutes  = 0;
                station.TimeUsedMinutes  = 0;
                station.IsPoweredOn      = false;
                station.SessionStartTime = null;
                station.UpdatedAt        = DateTime.UtcNow;

                _context.SaveChanges();

                return Json(new { success = true, message = "Session ended" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Activity & Reporting ───────────────────────────────────────────────

        /// <summary>Get player activity log for today.</summary>
        [HttpGet]
        public IActionResult GetActivityLog(int limit = 10)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

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
                        username  = a.Username,
                        action    = "Login",
                        timestamp = a.LastLogin,
                        game      = a.CurrentGame ?? "None",
                        duration  = (DateTime.UtcNow - (a.SessionStartTime ?? a.LastLogin!.Value)).TotalMinutes
                    })
                    .ToList();

                return Json(new { success = true, activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity log");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Player Admin Actions ───────────────────────────────────────────────

        /// <summary>Delete a player (admin action).</summary>
        [HttpDelete]
        public IActionResult DeletePlayer(int playerId)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var player = _context.Accounts.FirstOrDefault(a => a.Id == playerId);
                if (player == null)
                    return Json(new { success = false, message = "Player not found" });

                // Free any station the player is using
                var activeStation = _context.Stations.FirstOrDefault(s => s.CurrentPlayerId == playerId);
                if (activeStation != null)
                {
                    activeStation.Status           = "offline";
                    activeStation.CurrentPlayerId  = null;
                    activeStation.CurrentUser      = null;
                    activeStation.TimePaidMinutes  = 0;
                    activeStation.TimeUsedMinutes  = 0;
                    activeStation.IsPoweredOn      = false;
                    activeStation.SessionStartTime = null;
                    activeStation.UpdatedAt        = DateTime.UtcNow;
                }

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

        /// <summary>Update player balance (admin action).</summary>
        [HttpPost]
        public IActionResult UpdatePlayerBalance([FromBody] TopUpRequest request)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                var player = _context.Accounts.FirstOrDefault(a => a.Id == request.PlayerId);
                if (player == null)
                    return Json(new { success = false, message = "Player not found" });

                player.Balance += request.Amount;
                _context.Accounts.Update(player);

                // Record transaction
                _context.Transactions.Add(new Transaction {
                    Username = player.Username,
                    Type = "TopUp",
                    Amount = request.Amount,
                    StationInfo = player.CurrentStation
                });

                _context.SaveChanges();

                return Json(new { success = true, newBalance = player.Balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating balance");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class TopUpRequest {
            public int PlayerId { get; set; }
            public decimal Amount { get; set; }
        }

        /// <summary>Reset player password (admin action).</summary>
        [HttpPost]
        public IActionResult ResetPlayerPassword(int playerId, string newPassword)
        {
            var authCheck = RequireAdminJson();
            if (authCheck != null) return authCheck;

            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                    return Json(new { success = false, message = "Password cannot be empty" });

                var player = _context.Accounts.FirstOrDefault(a => a.Id == playerId);
                if (player == null)
                    return Json(new { success = false, message = "Player not found" });

                // FIX: hash the new password
                player.Password = PasswordHelper.Hash(newPassword);
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
    }
}