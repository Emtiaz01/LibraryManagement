using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Areas.CustomerArea.Controllers
{
    [Area("CustomerArea")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> productList = _context.Product.Include(p=>p.Category).ToList();
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            var product = _context.Product.Include(p => p.Category).FirstOrDefault(p => p.ProductId == id);
            return View(product);
        }
    }
}
