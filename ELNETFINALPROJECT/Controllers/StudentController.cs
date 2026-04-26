using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ELNETFINALPROJECT.Data;
using ELNETFINALPROJECT.Models.ViewModels;
using System.Security.Claims;

namespace ELNETFINALPROJECT.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StudentController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Dashboard()
        {
            var userId = User.FindFirstValue("UserId");
            var studentId = User.FindFirstValue("StudentId");
            var fullName = User.Identity?.Name ?? "";

            var model = new StudentDashboardViewModel
            {
                FullName = fullName,
                TotalRequests = _db.SitInRequests.Count(r => r.StudentId == studentId),
                ApprovedRequests = _db.SitInRequests.Count(r => r.StudentId == studentId && r.Status == "Approved"),
                PendingRequests = _db.SitInRequests.Count(r => r.StudentId == studentId && r.Status == "Pending"),
                RecentRequests = _db.SitInRequests.Where(r => r.StudentId == studentId).OrderByDescending(r=>r.DateTime).Take(5).ToList(),
                Notifications = _db.Notifications.Where(n => n.UserId.ToString() == userId).OrderByDescending(n=>n.Id).Take(5).ToList(),
                Labs = new List<ELNETFINALPROJECT.Models.Lab> {
                    new ELNETFINALPROJECT.Models.Lab { Room = "544", Status = "Available" },
                    new ELNETFINALPROJECT.Models.Lab { Room = "532", Status = "Occupied" },
                    new ELNETFINALPROJECT.Models.Lab { Room = "546", Status = "Available" }
                }
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult RequestSitIn()
        {
            // Provide available rooms to the view (replace with DB lookup in real app)
            ViewBag.Rooms = new List<ELNETFINALPROJECT.Models.Lab> {
                new ELNETFINALPROJECT.Models.Lab{ Room = "544", Status = "Available" },
                new ELNETFINALPROJECT.Models.Lab{ Room = "546", Status = "Available" },
                new ELNETFINALPROJECT.Models.Lab{ Room = "532", Status = "Occupied" }
            };

            return View();
        }

        [HttpPost]
        public IActionResult RequestSitIn(string room, DateTime date, string startTime, string endTime, string purpose)
        {
            var studentId = User.FindFirstValue("StudentId");
            if (string.IsNullOrEmpty(studentId)) return Forbid();

            var dt = date; // for simplicity use date and ignore times
            var request = new ELNETFINALPROJECT.Models.SitInRequest
            {
                StudentId = studentId,
                Room = room,
                DateTime = dt,
                Status = "Pending"
            };
            _db.SitInRequests.Add(request);
            _db.SaveChanges();

            TempData["Message"] = "Request submitted.";
            return RedirectToAction("MyRequests");
        }

        public IActionResult MyRequests()
        {
            var studentId = User.FindFirstValue("StudentId");
            var requests = _db.SitInRequests.Where(r => r.StudentId == studentId).OrderByDescending(r=>r.DateTime).ToList();
            return View(requests);
        }

        public IActionResult Notifications()
        {
            var userId = int.Parse(User.FindFirstValue("UserId"));
            var notifications = _db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n=>n.Id).ToList();
            return View(notifications);
        }

        public IActionResult Announcements()
        {
            var announcements = _db.Announcements.OrderByDescending(a=>a.DatePosted).ToList();
            return View(announcements);
        }
    }
}
