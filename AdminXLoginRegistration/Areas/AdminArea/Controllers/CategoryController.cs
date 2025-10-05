using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Show the list with all categories for Razor looping
        [HttpGet]
        public IActionResult Index()
        {
            var categories = _db.Category.ToList();
            return View(categories);
        }

        // For AJAX fetch (if required, e.g. for dynamic tables)
        [HttpGet]
        public IActionResult GetCategory()
        {
            var category = _db.Category.ToList();
            return new JsonResult(category);
        }

        [HttpGet]
        public IActionResult UpsertCategory(int? id)
        {
            var model = new Category();
            if (id == null || id == 0)
            {
                return View(model);
            }
            else
            {
                Category? category = _db.Category.FirstOrDefault(u => u.CategoryId == id);
                if (category == null)
                    return NotFound();
                else return View(category);
            }
        }

        [HttpPost]
        public IActionResult UpsertCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                var categoryId = category.CategoryId;
                if (categoryId != 0)
                {
                    _db.Category.Update(category);
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    _db.Category.Add(category);
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(category);
        }

        // AJAX Delete endpoint (used by delete button in Razor table)
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var obj = _db.Category.FirstOrDefault(u => u.CategoryId == id);
            if (obj == null)
                return Json(new { success = false, message = "Error while deleting" });
            _db.Category.Remove(obj);
            _db.SaveChanges();
            return Json(new { success = true, message = "Deleted successfully" });
        }
    }
}
