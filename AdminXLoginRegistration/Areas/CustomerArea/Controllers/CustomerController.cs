using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Areas.CustomerArea.Controllers
{
    [Area("CustomerArea")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _user;
        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> user)
        {
            _context = context;
            _user = user;
        }
        public IActionResult Index()
        {
            var userId = _user.GetUserId(User);
            ViewBag.CurrentUserId = userId;

            var viewModelList = _context.Product
                .Include(p => p.Category)
                .Select(p => new BookLoanViewModel
                {
                    Product = p,
                    BookLoan = _context.BookLoan
                        .FirstOrDefault(bl => bl.ProductId == p.ProductId && bl.UserId == userId && bl.ReturnDate == null),
                        NextAvailableDate = p.ProductQuantity == 0
                ? _context.BookLoan
                    .Where(bl => bl.ProductId == p.ProductId && bl.ReturnDate == null)
                    .OrderBy(bl => bl.DueDate)
                    .Select(bl => bl.DueDate)
                    .FirstOrDefault()
                : null
                })
                .ToList();

            return View(viewModelList);
        }
        public IActionResult Details(int id)
        {
            var product = _context.Product
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var userId = _user.GetUserId(User);
            ViewBag.CurrentUserId = userId;

            var existingLoan = _context.BookLoan
                .FirstOrDefault(bl => bl.ProductId == id && bl.UserId == userId && bl.ReturnDate == null);

            var nextAvailableDate = _context.BookLoan
        .Where(bl => bl.ProductId == id && bl.ReturnDate == null)
        .OrderBy(bl => bl.DueDate)
        .Select(bl => bl.DueDate)
        .FirstOrDefault();
            ViewBag.NextAvailableDate = nextAvailableDate;

            var viewModel = new BookLoanViewModel
            {
                Product = product,
                BookLoan = existingLoan 
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BorrowBook(BookLoanViewModel vm)
        {

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                var product = _context.Product.Include(p => p.Category)
                                              .FirstOrDefault(p => p.ProductId == vm.BookLoan.ProductId);

                vm.Product = product;
                return View("Details", vm);
            }

            var productToUpdate = _context.Product.Include(p => p.Category)
                                              .FirstOrDefault(p => p.ProductId == vm.BookLoan.ProductId);

            productToUpdate.ProductQuantity -= 1;
            vm.BookLoan.UserId = _user.GetUserId(User);
            _context.BookLoan.Add(vm.BookLoan);
            _context.SaveChanges();

            TempData["Success"] = "Book borrowed successfully!";
            return RedirectToAction("Index");
        }

    }
}
