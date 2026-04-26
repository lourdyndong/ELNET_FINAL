using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ELNETFINALPROJECT.Data;
using System.Security.Claims;

namespace ELNETFINALPROJECT.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Dashboard()
        {
            var model = new ELNETFINALPROJECT.Models.ViewModels.AdminDashboardViewModel
            {
                TotalRequests = _db.SitInRequests.Count(),
                PendingRequests = _db.SitInRequests.Count(r => r.Status == "Pending"),
                ApprovedRequests = _db.SitInRequests.Count(r => r.Status == "Approved"),
                RecentPendingRequests = _db.SitInRequests.Where(r => r.Status == "Pending").OrderByDescending(r => r.DateTime).Take(5).ToList()
            };

            return View(model);
        }

        public IActionResult ManageRequests()
        {
            var requests = _db.SitInRequests.OrderByDescending(r => r.DateTime).ToList();
            return View(requests);
        }

        public IActionResult ManageAnnouncements()
        {
            var announcements = _db.Announcements.OrderByDescending(a => a.DatePosted).ToList();
            return View(announcements);
        }

        public IActionResult Labs()
        {
            // For simplicity, labs are a static placeholder list; replace with DB-backed data later
            var labs = new List<ELNETFINALPROJECT.Models.Lab> {
                new ELNETFINALPROJECT.Models.Lab{ Room = "544", Status = "Available" },
                new ELNETFINALPROJECT.Models.Lab{ Room = "532", Status = "Occupied" },
                new ELNETFINALPROJECT.Models.Lab{ Room = "546", Status = "Available" }
            };
            return View(labs);
        }

        public IActionResult Users()
        {
            var users = _db.Users.OrderBy(u=>u.FullName).ToList();
            return View(users);
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
