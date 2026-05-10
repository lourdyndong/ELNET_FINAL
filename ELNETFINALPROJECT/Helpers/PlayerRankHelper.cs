using ELNETFINALPROJECT.Models;

namespace ELNETFINALPROJECT.Helpers
{
    /// <summary>
    /// Centralised rank calculation — shared between HomeController and PlayerController.
    /// Rank progression: Bronze → Silver → Gold → Platinum → Diamond → Legend
    /// </summary>
    public static class PlayerRankHelper
    {
        public static string GetRank(Account account)
        {
            var accountAgeInDays = (DateTime.UtcNow - account.CreatedAt).TotalDays;

            if (account.TotalSessions >= 100 && accountAgeInDays >= 90) return "Legend";
            if (account.TotalSessions >= 75  && accountAgeInDays >= 60) return "Diamond";
            if (account.TotalSessions >= 50  && accountAgeInDays >= 30) return "Platinum";
            if (account.TotalSessions >= 25  && accountAgeInDays >= 14) return "Gold";
            if (account.TotalSessions >= 10  && accountAgeInDays >= 7)  return "Silver";

            return "Bronze";
        }
    }
}
