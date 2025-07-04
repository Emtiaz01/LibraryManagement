using System.Diagnostics;
using AdminXLoginRegistration.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Areas.AdminArea.Controllers
{
    [Area("AdminArea")] 
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [Authorize]
        public IActionResult Index()
        {
            //if (!User.Identity.IsAuthenticated)
            //{
            //    return RedirectToPage("/Account/Register", new { area = "Identity" });
            //}

            return View();
        }

        public IActionResult OurServices()
        {
            return View();
        }
        public IActionResult ContactUs()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
