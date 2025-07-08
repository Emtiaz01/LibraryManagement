using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Hosting;
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
        public IActionResult Upsert(int? id)
        {
            ProductViewModel vm = new ProductViewModel()
            {
                Product = new Product(),
                CategoryList = _context.Category.Select(c => new SelectListItem
                {
                    Text = c.CategoryName,
                    Value = c.CategoryId.ToString()
                })
            };

            if (id == null || id == 0)
                return View(vm);
            else
            {
                var productInDb = _context.Product.FirstOrDefault(p => p.ProductId == id);
                if (productInDb == null)
                    return NotFound();

                vm.Product = productInDb;
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel vm, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _web.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    if (!Directory.Exists(productPath))
                        Directory.CreateDirectory(productPath);

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    vm.Product.ProductImage = @"\images\products\" + fileName;
                }
                else
                {
                    var productFromDb = _context.Product.AsNoTracking()
                                        .FirstOrDefault(p => p.ProductId == vm.Product.ProductId);
                    if (productFromDb != null)
                    {
                        vm.Product.ProductImage = productFromDb.ProductImage;
                    }
                }
                if (vm.Product.ProductId == 0)
                    _context.Product.Add(vm.Product);
                else
                    _context.Product.Update(vm.Product);

                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            vm.CategoryList = _context.Category.Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.CategoryId.ToString()
            });

            return View(vm);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var product = _context.Product.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }
            if (!string.IsNullOrEmpty(product.ProductImage))
            {
                var imagePath = Path.Combine(_web.WebRootPath, product.ProductImage.TrimStart('\\'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Product.Remove(product);
            _context.SaveChanges();

            return Json(new { success = true, message = "Product deleted successfully." });
        }
    }
}
