using Microsoft.AspNetCore.Mvc;

namespace YourApp.Controllers
{
	public class GamesController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}