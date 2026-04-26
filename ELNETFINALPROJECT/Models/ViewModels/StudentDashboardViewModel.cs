using System.Collections.Generic;

namespace ELNETFINALPROJECT.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string FullName { get; set; } = "";
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int PendingRequests { get; set; }

        public List<ELNETFINALPROJECT.Models.SitInRequest> RecentRequests { get; set; } = new();
        public List<ELNETFINALPROJECT.Models.Notification> Notifications { get; set; } = new();
        public List<ELNETFINALPROJECT.Models.Lab> Labs { get; set; } = new();
    }
}
