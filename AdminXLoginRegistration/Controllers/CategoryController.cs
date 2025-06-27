using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LibraryManagementSystem.Controllers
{
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
            if(id==null || id==0)
            {
                return View();
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
            if(ModelState.IsValid)
            {
                var categoryId = category.CategoryId;
                if(categoryId!=0)
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
        public IActionResult Delete(int? id)
        {
            if(id ==null || id==0)
            {
                return NotFound();
            }
            Category? category = _db.Category.FirstOrDefault(u=>u.CategoryId==id);

            if(category == null)
            {
                return NotFound();
            }
            return View(category);  
        }
        [HttpPost,ActionName("Delete")]
        public IActionResult DeletePost(int id)
        {
            Category? obj = _db.Category.FirstOrDefault(u => u.CategoryId == id);
            if(obj == null) { NotFound(); }
            _db.Category.Remove(obj);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
