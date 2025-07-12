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
            IEnumerable<Product> productList = _context.Product.Include(p=>p.Category).ToList();
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            var product = _context.Product.Include(p => p.Category).FirstOrDefault(p => p.ProductId == id);
            if(product == null)
            {
                return NotFound();
            }
            var viewModel = new BookLoanViewModel
            {
                Product = product,
                BookLoan = new BookLoan
                {
                    ProductId = product.ProductId,
                    UserId = _user.GetUserId(User),
                }
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BorrowBook(BookLoanViewModel vm)
        {
            
            if (!ModelState.IsValid)
            {
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
