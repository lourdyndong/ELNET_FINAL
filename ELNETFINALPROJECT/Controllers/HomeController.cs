using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ELNETFINALPROJECT.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult Players()
        {
            return View();
        }
        public IActionResult Stations()
        {
            return View();
        }
        public IActionResult Games()
        {
            return View();
        }
    }
}
