using System.Collections.Generic;
using ELNETFINALPROJECT.Models;

namespace ELNETFINALPROJECT.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }

        public List<SitInRequest> RecentPendingRequests { get; set; } = new();
    }
}
