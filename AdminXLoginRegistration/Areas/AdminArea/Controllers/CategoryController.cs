using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
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
            return View();
        }
        [HttpGet]
        public IActionResult Delete(int id) {
            var obj = _db.Category.FirstOrDefault(u => u.CategoryId == id);
            if (obj == null)
            {
                return NotFound();
            }
            return View(obj);
        }
        
        [HttpPost("AdminArea/Category/DeleteIT/{id}")]
        public IActionResult DeleteIT(int id)
        {
            var obj = _db.Category.FirstOrDefault(u => u.CategoryId == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _db.Category.Remove(obj);
            _db.SaveChanges();
            return Json(new { success = true, message = "Deleted successfully" });
        }

    }

}
