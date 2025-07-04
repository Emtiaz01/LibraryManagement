using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Areas.CustomerArea.Controllers
{
    [Area("CustomerArea")]
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
