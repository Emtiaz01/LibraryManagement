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
            ViewBag.Categories = _context.Category.ToList();
            return View();
        }

        [HttpGet]
        public IActionResult GetProduct(string filter = "all", string searchText = "", int? categoryId = null)
        {
            var qs = _context.Product.Include(p => p.Category).AsQueryable();

            switch (filter)
            {
                case "premium": qs = qs.Where(p => p.IsPremium); break;
                case "donated": qs = qs.Where(p => p.IsDonated); break;
                case "instock": qs = qs.Where(p => p.ProductQuantity > 0); break;
                case "stockout": qs = qs.Where(p => p.ProductQuantity == 0); break;

            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var lower = searchText.ToLower();
                qs = qs.Where(p =>
                    (p.ProductName ?? "").ToLower().Contains(lower) ||
                    (p.ProductAuthor ?? "").ToLower().Contains(lower) ||
                    (p.Description ?? "").ToLower().Contains(lower) ||
                    (p.Category.CategoryName ?? "").ToLower().Contains(lower) ||
                    (p.ProductISBN ?? "").ToLower().Contains(lower));
            }

            if (categoryId.HasValue)
                qs = qs.Where(p => p.CategoryId == categoryId.Value);

            var products = qs.Select(p => new
            {
                productId = p.ProductId,
                productName = p.ProductName,
                productISBN = p.ProductISBN,
                productAuthor = p.ProductAuthor,
                description = p.Description,
                productImage = p.ProductImage,
                category = new { categoryName = p.Category != null ? p.Category.CategoryName : "" },
                productQuantity = p.ProductQuantity,
                productPrice = p.ProductPrice,
                isDonated = p.IsDonated
            }).ToList();

            return Json(products);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var product = _context.Product
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            var loanRecords = _context.BookLoan
                .Where(bl => bl.ProductId == id)
                .ToList();

            var vm = new ProductViewModel
            {
                Product = product,
                LoanRecords = loanRecords
            };

            return View(vm);
        }

[HttpGet]
public IActionResult Upsert(int? id)
{
    Product product;
    bool isEdit = id.HasValue && id.Value != 0;
    bool isDonated = false;

    if (isEdit)
    {
        product = _context.Product.FirstOrDefault(p => p.ProductId == id);
        if (product == null)
            return NotFound();

        if (product.IsDonated)
            isDonated = true;
    }
    else
    {
        product = new Product();
    }

    var vm = new ProductViewModel
    {
        Product = product,
        CategoryList = _context.Category.Select(c => new SelectListItem
        {
            Text = c.CategoryName,
            Value = c.CategoryId.ToString()
        })
    };

    ViewBag.IsDonated = isDonated;
    return View(vm);
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel vm, IFormFile? file)
        {
            // Prevent submit if product is donated
            if (vm.Product.ProductId != 0)
            {
                var original = _context.Product.FirstOrDefault(p => p.ProductId == vm.Product.ProductId);
                if (original != null && original.IsDonated)
                {
                    TempData["debug_errors"] = "Donated book cannot be edited!";
                    return RedirectToAction("Index", "Product", new { area = "AdminArea" });
                }
            }

            if (ModelState.IsValid)
            {
                string imagePath = vm.Product.ProductImage;

                if (file != null)
                {
                    string wwwRootPath = _web.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, "images");

                    // If a new file is uploaded and an old image exists, delete the old one
                    if (!string.IsNullOrEmpty(vm.Product.ProductImage))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, vm.Product.ProductImage.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save the new image
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    imagePath = "/images/" + fileName;
                }

                if (vm.Product.ProductId == 0)
                {
                    vm.Product.ProductImage = imagePath;
                    _context.Product.Add(vm.Product);
                }
                else
                {
                    // Correct fix: update tracked entity
                    var dbProduct = _context.Product.FirstOrDefault(p => p.ProductId == vm.Product.ProductId);
                    if (dbProduct == null)
                        return NotFound();

                    dbProduct.ProductName = vm.Product.ProductName;
                    dbProduct.ProductAuthor = vm.Product.ProductAuthor;
                    dbProduct.Description = vm.Product.Description;
                    dbProduct.ProductISBN = vm.Product.ProductISBN;
                    dbProduct.ProductPrice = vm.Product.ProductPrice;
                    dbProduct.CategoryId = vm.Product.CategoryId;
                    dbProduct.ProductQuantity = vm.Product.ProductQuantity;

                    dbProduct.ProductImage = imagePath;
                }

                _context.SaveChanges();
                return RedirectToAction("Index", "Product", new { area = "AdminArea" });
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
                return Json(new { success = false, message = "Product not found." });

            // Check for related BookLoan records
            var hasLoans = _context.BookLoan.Any(bl => bl.ProductId == id);
            if (hasLoans)
                return Json(new { success = false, message = "Cannot delete. Product has related loan or payment records." });

            _context.Product.Remove(product);
            _context.SaveChanges();
            return Json(new { success = true, message = "Product deleted successfully." });
        }


    }
}
