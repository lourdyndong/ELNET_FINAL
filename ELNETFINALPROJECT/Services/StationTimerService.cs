using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ELNETFINALPROJECT.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ELNETFINALPROJECT.Services
{
    public class StationTimerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StationTimerService> _logger;

        public StationTimerService(IServiceProvider serviceProvider, ILogger<StationTimerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Station Timer Service is starting.");

            // On startup, catch up any missed deductions for active sessions
            await CatchUpDeductions(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Run every 3 minutes — deduct ₱1 per tick (₱20/hr = ₱1 per 3 min)
                await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var activeStations = context.Stations.Where(s => s.Status == "active").ToList();

                        foreach (var station in activeStations)
                        {
                            // Increment used time by 3 minutes
                            station.TimeUsedMinutes += 3;

                            bool isExpired = false;

                            // Handle registered player deduction
                            if (station.CurrentPlayerId.HasValue)
                            {
                                var player = context.Accounts.FirstOrDefault(a => a.Id == station.CurrentPlayerId.Value);
                                if (player != null)
                                {
                                    // Deduct ₱1 every 3 minutes (= ₱20/hr)
                                    player.Balance -= 1m;
                                    if (player.Balance < 0) player.Balance = 0;

                                    _logger.LogInformation($"PC {station.StationNumber}: Deducted ₱1 from {player.Username}. New balance: ₱{player.Balance:F2}");

                                    // Update station's TimePaidMinutes to sync with new balance
                                    station.TimePaidMinutes = (int)((player.Balance / 20m) * 60m) + station.TimeUsedMinutes;

                                    if (player.Balance <= 0)
                                    {
                                        isExpired = true;
                                        player.CurrentStation = null;
                                        player.SessionStartTime = null;
                                        player.Status = "Offline";
                                    }
                                }
                            }
                            else // Guest
                            {
                                if (station.TimeUsedMinutes >= station.TimePaidMinutes)
                                {
                                    isExpired = true;
                                }
                            }

                            // Check if time is up
                            if (isExpired)
                            {
                                _logger.LogInformation($"PC {station.StationNumber} session expired.");
                                station.Status = "offline";
                                station.CurrentUser = null;
                                station.CurrentPlayerId = null;
                                station.TimePaidMinutes = 0;
                                station.TimeUsedMinutes = 0;
                                station.IsPoweredOn = false;
                                station.SessionStartTime = null;
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in StationTimerService");
                }
            }

            _logger.LogInformation("Station Timer Service is stopping.");
        }

        /// <summary>
        /// On server restart, calculate how many 3-minute ticks were missed
        /// and deduct the correct amount from each active player's balance.
        /// </summary>
        private async Task CatchUpDeductions(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var activeStations = context.Stations.Where(s => s.Status == "active" && s.SessionStartTime != null).ToList();

                foreach (var station in activeStations)
                {
                    var elapsedMinutes = (int)(DateTime.UtcNow - station.SessionStartTime!.Value).TotalMinutes;
                    var ticksShouldHaveHappened = elapsedMinutes / 3;
                    var ticksAlreadyApplied = station.TimeUsedMinutes / 3;
                    var missedTicks = ticksShouldHaveHappened - ticksAlreadyApplied;

                    if (missedTicks <= 0) continue;

                    // Update station time
                    station.TimeUsedMinutes = ticksShouldHaveHappened * 3;

                    if (station.CurrentPlayerId.HasValue)
                    {
                        var player = context.Accounts.FirstOrDefault(a => a.Id == station.CurrentPlayerId.Value);
                        if (player != null)
                        {
                            // Deduct ₱1 per missed tick
                            decimal totalDeduction = missedTicks;
                            player.Balance -= totalDeduction;
                            if (player.Balance < 0) player.Balance = 0;

                            _logger.LogInformation($"PC {station.StationNumber}: Caught up {missedTicks} missed ticks. Deducted ₱{totalDeduction:F2} from {player.Username}. New balance: ₱{player.Balance:F2}");

                            station.TimePaidMinutes = (int)((player.Balance / 20m) * 60m) + station.TimeUsedMinutes;

                            if (player.Balance <= 0)
                            {
                                _logger.LogInformation($"PC {station.StationNumber} session expired during catch-up.");
                                player.CurrentStation = null;
                                player.SessionStartTime = null;
                                player.Status = "Offline";

                                station.Status = "offline";
                                station.CurrentUser = null;
                                station.CurrentPlayerId = null;
                                station.TimePaidMinutes = 0;
                                station.TimeUsedMinutes = 0;
                                station.IsPoweredOn = false;
                                station.SessionStartTime = null;
                            }
                        }
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error catching up deductions on startup");
            }
        }
    }
}
