using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq.Expressions;

namespace LibraryManagementSystem.Areas.CustomerArea.Controllers
{
    [Area("CustomerArea")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _user;
        private readonly IEmailService _emailService;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> user, IEmailService emailService)
        {
            _context = context;
            _user = user;
            _emailService = emailService;
        }
        [HttpGet]
        public async Task<IActionResult> FilterBooks(string filter = "all", int? categoryId = null, string author = "", string searchText = "")
        {
            var userId = _user.GetUserId(User);
            var currentUser = await _user.GetUserAsync(User);

            var qs = _context.Product.Include(p => p.Category).AsQueryable();

            switch (filter)
            {
                case "donated": qs = qs.Where(p => p.IsDonated); break;
                case "instock": qs = qs.Where(p => p.ProductQuantity > 0); break;
                case "stockout": qs = qs.Where(p => p.ProductQuantity == 0); break;
                case "premium": qs = qs.Where(p => p.IsPremium); break;
            }

            if (categoryId.HasValue && categoryId.Value > 0)
                qs = qs.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(author))
                qs = qs.Where(p => p.ProductAuthor.ToLower().Contains(author.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(searchText))
                qs = qs.Where(p => p.ProductName.ToLower().Contains(searchText.Trim().ToLower()));

            // Show ALL books, but indicate premium in view
            var products = qs.Where(p => p.DonationStatus == "Approved" || !p.IsDonated)
                .OrderByDescending(p => p.ProductId)
                .ToList();

            var viewModelList = products.Select(p => new LibraryManagementSystem.ViewModel.BookLoanViewModel
            {
                Product = p,
                BookLoan = _context.BookLoan
                    .Where(bl => bl.ProductId == p.ProductId && bl.UserId == userId &&
                                 (bl.Status == LoanStatus.Pending ||
                                  bl.Status == LoanStatus.Approved ||
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
            }).ToList();

            // Pass subscription info for view logic
            ViewBag.HasPremium = (currentUser != null && currentUser.IsSubscribed && currentUser.SubscriptionEndDate > DateTime.Now);

            ViewBag.CurrentUserId = userId;
            return PartialView("_BookCardsAjax", viewModelList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayPremium(string Plan)
        {
            // Save selection to TempData, Session, or Pass as route/parameter to payment page
            TempData["PremiumPlan"] = Plan;
            return RedirectToAction("SubscriptionPayment", "Payment", new { area = "CustomerArea", plan = Plan });
        }



        // --- DONATE BOOK FEATURE START ---
        [HttpGet]
        public IActionResult DonateBook()
        {
            ViewBag.Categories = _context.Category.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DonateBook(LibraryManagementSystem.ViewModel.DonateBookViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Category.ToList();
                return View(vm);
            }

            string fileName = null;
            if (vm.ProductImageFile != null && vm.ProductImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "books");
                Directory.CreateDirectory(uploadsFolder);
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ProductImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    vm.ProductImageFile.CopyTo(stream);
                }
            }

            var donatedProduct = new Product
            {
                ProductName = vm.ProductName,
                Description = vm.Description,
                ProductISBN = vm.ProductISBN,
                ProductAuthor = vm.ProductAuthor,
                ProductQuantity = vm.ProductQuantity,
                ProductPrice = 0,
                CategoryId = vm.CategoryId,
                ProductImage = fileName != null ? "/images/books/" + fileName : null,
                IsDonated = true,
                DonationStatus = "Pending",
                DonorName = vm.DonorName,
                DonorEmail = vm.DonorEmail,
                DonationDate = DateTime.Now
            };

            _context.Product.Add(donatedProduct);
            _context.SaveChanges();

            TempData["Success"] = "Thank you for your generous book donation! It is pending admin approval.";
            return RedirectToAction("Index");
        }
        // --- DONATE BOOK FEATURE END ---

        public async Task<IActionResult> Index()
        {
            var userId = _user.GetUserId(User);
            var currentUser = await _user.FindByIdAsync(userId);
            if (currentUser == null) return Challenge();

            ViewBag.HasPremium = currentUser.IsSubscribed && currentUser.SubscriptionEndDate > DateTime.Now;
            ViewBag.CurrentUserId = userId;

            // --- Fine Calculation & User Blocking Logic ---
            var userLoans = _context.BookLoan
                .Include(bl => bl.User)
                .Where(bl => bl.UserId == userId && bl.ReturnDate == null && bl.DueDate < DateTime.Now)
                .ToList();

            double totalUnpaidFine = 0;
            double fineRate = (currentUser.IsSubscribed && currentUser.SubscriptionEndDate > DateTime.Now) ? 25.0 : 50.0;

            foreach (var loan in userLoans)
            {
                var overdueDays = (DateTime.Now - loan.DueDate).Days;
                if (overdueDays > 0)
                {
                    loan.FineAmount = overdueDays * fineRate;
                    _context.BookLoan.Update(loan);

                    // Check if a fine for this specific loan has been paid
                    bool isPaid = _context.Payment.Any(p => p.BookLoanId == loan.BookLoanId && p.Status == "Paid");
                    if (!isPaid)
                    {
                        totalUnpaidFine += loan.FineAmount;
                    }
                }
            }

            // Only block for fines
            if (totalUnpaidFine >= 500)
            {
                currentUser.IsBlockedFromBorrowing = true;
            }
            else
            {
                currentUser.IsBlockedFromBorrowing = false;
            }

            _context.Users.Update(currentUser);
            await _context.SaveChangesAsync();
            // --- End of Fine/Blocking Logic ---

            var viewModelList = _context.Product
                .Include(p => p.Category)
                .Where(p => p.DonationStatus == "Approved" || !p.IsDonated)
                .Select(p => new BookLoanViewModel
                {
                    Product = p,
                    BookLoan = _context.BookLoan
                        .Where(bl => bl.ProductId == p.ProductId && bl.UserId == userId &&
                                 (bl.Status == LoanStatus.Pending ||
                                  bl.Status == LoanStatus.Approved ||
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

            // Fetch any active loan (to show alert if needed), but DO NOT RETURN EARLY
            var existingLoan = _context.BookLoan
                .FirstOrDefault(bl => bl.ProductId == id && bl.UserId == userId &&
                                      (bl.Status == LoanStatus.Pending || bl.Status == LoanStatus.Approved));

            // Fine calculation logic remains unchanged if desired


            var nextAvailableDate = _context.BookLoan
                .Where(bl => bl.ProductId == id && bl.ReturnDate == null)
                .OrderBy(bl => bl.DueDate)
                .Select(bl => bl.DueDate)
                .FirstOrDefault();
            ViewBag.NextAvailableDate = nextAvailableDate;
            ViewBag.HasExistingLoan = existingLoan != null; // Pass this info to the view

            var viewModel = new BookLoanViewModel
            {
                Product = product,
                BookLoan = existingLoan
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorrowBookAjax(int productId, string borrowDate)
        {
            try
            {


                var currentUser = await _user.GetUserAsync(User);
                ViewBag.IsBlocked = currentUser?.IsBlockedFromBorrowing ?? false;

                if (!User.Identity.IsAuthenticated)
                    return Json(new { success = false, message = "You must be logged in to borrow a book." });

                var userId = _user.GetUserId(User);
                var user = await _user.FindByIdAsync(userId);

                if (user.IsBlockedFromBorrowing)
                    return Json(new { success = false, message = "You are blocked from borrowing due to unpaid fines. Please pay your fines to reactivate your account." });

                var product = _context.Product.FirstOrDefault(p => p.ProductId == productId);
                if (product == null)
                    return Json(new { success = false, message = "Book not found." });

                // Check if user already has a pending/approved loan
                var existingLoan = _context.BookLoan
                    .FirstOrDefault(bl => bl.ProductId == productId && bl.UserId == userId &&
                                          (bl.Status == LoanStatus.Pending || bl.Status == LoanStatus.Approved));
                if (existingLoan != null)
                    return Json(new { success = false, message = "You already requested or borrowed this book." });

                // Parse borrow date from user POST
                DateTime borrowDt;
                if (!DateTime.TryParse(borrowDate, out borrowDt))
                    return Json(new { success = false, message = "Invalid borrow date." });

                if (borrowDt < DateTime.Today)
                    return Json(new { success = false, message = "Borrow date cannot be in the past." });

                // *** Membership-based due date ***
                bool isPremium = user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now;
                DateTime dueDate = isPremium ? borrowDt.AddDays(14) : borrowDt.AddDays(7);

                // Create a new BookLoan (request)
                var newLoan = new BookLoan
                {
                    ProductId = productId,
                    UserId = userId,
                    BorrowDate = borrowDt,
                    DueDate = dueDate,
                    Status = LoanStatus.Pending,
                    FineAmount = 0
                };

                _context.BookLoan.Add(newLoan);
                _context.SaveChanges();

                // Send email to admin
                var userName = user?.UserName ?? "User";
                var adminEmail = "emtiuz.emon@gmail.com"; // update to your admin email

                string emailBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>New Borrow Request</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border: 1px solid #ddd;
            border-radius: 8px;
            box-shadow: 0 4px 8px rgba(0,0,0,0.1);
        }}
        .header {{
            background-color: #002E6D;
            color: #ffffff;
            padding: 15px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .content {{
            padding: 25px 20px;
        }}
        .content p {{
            margin: 0 0 15px;
        }}
        .details-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        .details-table th, .details-table td {{
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #eaeaea;
        }}
        .details-table th {{
            background-color: #f9f9f9;
            font-weight: 600;
            width: 35%;
        }}
        .footer {{
            text-align: center;
            font-size: 12px;
            color: #777;
            padding: 15px;
            border-top: 1px solid #ddd;
            margin-top: 20px;
        }}
        .action-button {{
            display: inline-block;
            background-color: #1565c0;
            color: white;
            padding: 12px 25px;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            margin-top: 15px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Book Borrow Request</h1>
        </div>
        <div class=""content"">
            <p>Hello Admin,</p>
            <p>A new request to borrow a book has been submitted. Please review the details below and take the necessary action.</p>
            
            <table class=""details-table"">
                <tr>
                    <th>User</th>
                    <td>{System.Security.SecurityElement.Escape(userName)}</td>
                </tr>
                <tr>
                    <th>User Email</th>
                    <td><a href=""mailto:{System.Security.SecurityElement.Escape(user.Email)}"">{System.Security.SecurityElement.Escape(user.Email)}</a></td>
                </tr>
                <tr>
                    <th>Book Title</th>
                    <td><strong>{System.Security.SecurityElement.Escape(product.ProductName)}</strong></td>
                </tr>
                <tr>
                    <th>Book Author</th>
                    <td>{System.Security.SecurityElement.Escape(product.ProductAuthor)}</td>
                </tr>
                <tr>
                    <th>ISBN</th>
                    <td>{System.Security.SecurityElement.Escape(product.ProductISBN)}</td>
                </tr>
                <tr>
                    <th>Requested Borrow Date</th>
                    <td>{borrowDt:MMMM dd, yyyy}</td>
                </tr>
                <tr>
                    <th>Calculated Due Date</th>
                    <td>{dueDate:MMMM dd, yyyy}</td>
                </tr>
            </table>

            <p style=""text-align:center;"">
                <a href=""#"" class=""action-button"">Go to Admin Dashboard</a>
            </p>
        </div>
        <div class=""footer"">
            <p>This is an automated notification from the BS Library Management System.</p>
            <p>&copy; {DateTime.Now.Year} BS Library. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                await _emailService.SendEmailAsync(
                    adminEmail,
                    $"New Borrow Request: {product.ProductName}", // More descriptive subject
                    emailBody
                );


                return Json(new { success = true, message = "Your borrow request has been sent to the admin." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message + " | " + ex.StackTrace });
            }
        }

            [HttpGet]
            public IActionResult MyBorrowedBooks(string filter = "latest")
            {
                var userId = _user.GetUserId(User);
                var currentUser = _user.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.HasPremium = (currentUser != null && currentUser.IsSubscribed && currentUser.SubscriptionEndDate > DateTime.Now);


                var myLoans = _context.BookLoan.Include(bl => bl.Product)
                    .Where(bl => bl.UserId == userId);

                // Filtering logic (do not ToList() until after filtering for efficiency)
                switch (filter)
                {
                    case "oldest":
                        myLoans = myLoans.OrderBy(bl => bl.BorrowDate);
                        break;
                    case "returned":
                        myLoans = myLoans.Where(bl => bl.ReturnDate != null).OrderByDescending(bl => bl.BorrowDate);
                        break;
                    case "notreturned":
                        myLoans = myLoans.Where(bl => bl.ReturnDate == null).OrderByDescending(bl => bl.BorrowDate);
                        break;
                    case "finepaid":
                        myLoans = myLoans.Where(bl =>
                            _context.Payment.Any(p => p.BookLoanId == bl.BookLoanId && p.Status == "Paid")
                        ).OrderByDescending(bl => bl.BorrowDate);
                        break;
                    case "finenotpaid":
                        myLoans = myLoans.Where(bl =>
                            (bl.FineAmount > 0 || (bl.ReturnDate == null && bl.DueDate < DateTime.Now)) &&
                            !_context.Payment.Any(p => p.BookLoanId == bl.BookLoanId && p.Status == "Paid")
                        ).OrderByDescending(bl => bl.BorrowDate);
                        break;

                    default:
                        myLoans = myLoans.OrderByDescending(bl => bl.BorrowDate); // "latest"
                        break;
                }
                ViewBag.CurrentFilter = filter;
                return View(myLoans.ToList());
            }
        
        


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReturnBook(int? BookLoanId)
        {
            var loan = _context.BookLoan
                .FirstOrDefault(bl => bl.BookLoanId == BookLoanId && bl.UserId == _user.GetUserId(User) && bl.ReturnDate == null);

            if (loan == null)
            {
                return NotFound();
            }
            loan.Status = LoanStatus.ReturnPending;
            _context.SaveChanges();
            TempData["Success"] = "Your Return Request Has Been Sent Successfully!";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult SubscribeView()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe()
        {
            var currentUser = await _user.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            currentUser.IsSubscribed = true;
            currentUser.SubscriptionEndDate = DateTime.Now.AddMonths(1);
            _context.Users.Update(currentUser);
            await _context.SaveChangesAsync();
            TempData["Success"] = "You are now a premium member!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearHistory()
        {
            var userId = _user.GetUserId(User);
            // Remove payments first if needed
            var userPayments = _context.Payment.Where(p => p.UserId == userId).ToList();
            _context.Payment.RemoveRange(userPayments);

            // Remove book loans for this user
            var userLoans = _context.BookLoan.Where(bl => bl.UserId == userId).ToList();
            _context.BookLoan.RemoveRange(userLoans);

            _context.SaveChanges();
            return RedirectToAction("MyBorrowedBooks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FilterBooks(string filterOption, string categoryId, string author, string searchText)
        {
            var userId = _user.GetUserId(User);
            var query = _context.Product.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(categoryId))
                query = query.Where(p => p.CategoryId.ToString() == categoryId);

            if (!string.IsNullOrEmpty(author))
                query = query.Where(p => p.ProductAuthor.Contains(author));

            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(p => p.ProductName.Contains(searchText));

            switch (filterOption)
            {
                case "donated":
                    query = query.Where(p => p.IsDonated);
                    break;
                case "instock":
                    query = query.Where(p => p.ProductQuantity > 0);
                    break;
                case "stockout":
                    query = query.Where(p => p.ProductQuantity == 0);
                    break;
                case "premium":
                    query = query.Where(p => p.IsPremium);
                    break;
            }

            var viewModelList = query.Select(p => new BookLoanViewModel
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
            }).ToList();

            // Calculate fines
            foreach (var vm in viewModelList)
            {
                var loan = vm.BookLoan;
                if (loan != null && loan.DueDate < DateTime.Now && loan.ReturnDate == null)
                {
                    var overdueDays = (DateTime.Now - loan.DueDate).Days;
                    if (overdueDays > 0)
                        loan.FineAmount = overdueDays * 1.0;
                }
            }

            ViewBag.IsBlocked = _context.Users.Find(userId)?.IsBlockedFromBorrowing ?? false;
            return PartialView("_BookCardsAjax", viewModelList);
        }
    }
}
