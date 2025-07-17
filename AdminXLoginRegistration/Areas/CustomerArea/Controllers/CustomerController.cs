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
                    .Where(bl => bl.ProductId == p.ProductId && bl.UserId == userId &&
                     (bl.Status == LoanStatus.Pending || 
                     bl.Status == LoanStatus.Approved ||
                     bl.Status == LoanStatus.Rejected ||
                     bl.Status == LoanStatus.ReturnPending))
                        .OrderByDescending(bl => bl.BookLoanId)
                        .FirstOrDefault(),
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
            if(!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "You must be logged in to borrow a book.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
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


            vm.BookLoan.UserId = _user.GetUserId(User);
            if(vm.BookLoan.Status!= LoanStatus.Pending)
            {
                vm.BookLoan.Status = LoanStatus.Pending;
            }
            _context.BookLoan.Add(vm.BookLoan);
            _context.SaveChanges();

            TempData["Success"] = "You Borrow Request has sent to Admin";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReturnBook(int? BookLoanId)
        {
            var loan = _context.BookLoan
                .FirstOrDefault(bl => bl.BookLoanId == BookLoanId && bl.UserId == _user.GetUserId(User) && bl.ReturnDate==null);

            if (loan == null)
            {
                return NotFound();
            }
            loan.Status = LoanStatus.ReturnPending;


            _context.SaveChanges();
            TempData["Success"] = "Your Return Request Has Been Sent Successfully!";
            return RedirectToAction("Index");
        }
    }
}
