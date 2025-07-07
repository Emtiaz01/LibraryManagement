using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Areas.AdminArea.Controllers
{
    [Area("AdminArea")] 

    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _web;
        public ProductController(ApplicationDbContext context, IWebHostEnvironment web)
        {
            _context = context;
            _web = web;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult GetProduct()
        {
            var product = _context.Product.Include(p => p.Category).ToList();
            if (product == null || !product.Any())
            {
                return NotFound("No products found.");
            }
            return new JsonResult(product);
        }
        [HttpGet]
        public IActionResult UpsertProduct(int? id)
        {
            var model = new ProductViewModel
            {
                CategoryList = _context.Category
                    .Select(c => new SelectListItem
                    {
                        Text = c.CategoryName,
                        Value = c.CategoryId.ToString()
                    }),
                Product = id == null ? new Product() :
                          _context.Product.FirstOrDefault(p => p.ProductId == id) ?? new Product()
            };
            return View(model);
        }

        //[HttpPost]
        //public IActionResult UpsertProduct(ProductViewModel model,IFormFile? file)
        
        //}
    }
}
