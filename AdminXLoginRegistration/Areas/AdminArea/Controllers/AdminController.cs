using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagementSystem.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    // [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _user;
        private readonly RoleManager<IdentityRole> _role;
        private readonly IEmailService _emailService;

        public bool isPremiumMember { get; private set; }

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> user,
            RoleManager<IdentityRole> role, IEmailService emailService)
        {
            _user = user;
            _role = role;
            _context = context;
            _emailService = emailService;
        }


        [HttpGet]
        public IActionResult Index()
        {
            // Get all users (adjust to fit your model/data as needed)
            var users = _context.Users.ToList();
            return View(users);
        }
        public IActionResult GetProduct(string filter = "all", string searchText = "", int? categoryId = null)
        {
            var query = _context.Product.Include(p => p.Category).AsQueryable();

            switch (filter)
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
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var lowerSearch = searchText.ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(lowerSearch) ||
                    p.ProductAuthor.ToLower().Contains(lowerSearch) ||
                    p.Category.CategoryName.ToLower().Contains(lowerSearch));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
            var products = query.ToList();
            return new JsonResult(products);
        }


        [HttpGet]
        public IActionResult DonatedBooks()
        {
            var books = _context.Product
                .Include(p => p.Category)
                .Where(p => p.IsDonated && p.DonationStatus == "Approved")
                .OrderByDescending(p => p.ProductId)
                .ToList();
            return View(books);
        }


        [HttpGet]
        public IActionResult Dashboard()
        {
            var today = DateTime.Today;

            int pendingDonations = _context.Product.Count(p => p.IsDonated && p.DonationStatus == "Pending");
            int membersWithHighFine = _context.Users
                .Count(u => _context.Payment.Where(p => p.UserId == u.Id && p.Status == "Unpaid").Sum(p => p.Amount) >= 200);

            int newMembersToday = 0;
            int donatedBooksCount = _context.Product.Count(p => p.IsDonated && p.DonationStatus == "Approved");
            int booksIssuedToday = _context.BookLoan.Count(b => b.BorrowDate.Date == today);
            int booksReturnedToday = _context.BookLoan.Count(b => b.ReturnDate.HasValue && b.ReturnDate.Value.Date == today);

            // Most borrowed book:
            var mostBorrowedBook = _context.BookLoan
                .GroupBy(b => b.ProductId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.FirstOrDefault().Product.ProductName)
                .FirstOrDefault();

            // Overdue loans count (NEW)
            int overdueLoans = _context.BookLoan
                .Count(bl => bl.Status == LoanStatus.Approved && bl.DueDate < today);

            // Activities merge
            var loanActivities = _context.BookLoan
                .OrderByDescending(b => b.BorrowDate)
                .Include(b => b.User)
                .Include(b => b.Product)
                .Take(7)
                .AsEnumerable()
                .Select(b =>
                    (b.User != null && b.Product != null)
                    ? $"{b.User.Email} borrowed {b.Product.ProductName} on {b.BorrowDate:dd MMM yyyy}"
                    : null
                )
                .Where(x => x != null);

            var donationActivities = _context.Product
                .Where(p => p.IsDonated && p.DonationStatus == "Approved" && (p.DonorEmail != null || p.DonorName != null))
                .OrderByDescending(p => p.DonationDate)
                .Take(7)
                .Select(p =>
                    $"{(string.IsNullOrEmpty(p.DonorEmail) ? (p.DonorName ?? "Anonymous") : p.DonorEmail)} donated {p.ProductName} on {p.DonationDate:dd MMM yyyy}"
                );

            var mergedActivities = loanActivities
                .Concat(donationActivities)
                .Take(10)
                .ToList();

            var allBooks = _context.Product.Include(p => p.Category).ToList();

            var vm = new AdminDashboardViewModel
            {
                TotalBooks = _context.Product.Any() ? _context.Product.Sum(p => p.ProductQuantity) : 0,
                TotalMembers = _context.Users.Count(),
                ActiveLoans = _context.BookLoan.Count(b => b.Status == LoanStatus.Approved),
                PendingDonations = pendingDonations,
                MembersWithHighFine = membersWithHighFine,
                BooksIssuedToday = booksIssuedToday,
                BooksReturnedToday = booksReturnedToday,
                BooksReserved = 0,
                NewMembersToday = newMembersToday,
                DonatedBooksCount = donatedBooksCount,
                RecentActivities = mergedActivities,
                AllBooks = allBooks,
                OverdueLoans = overdueLoans,   // <---- Added this line!
                MostBorrowedBook = mostBorrowedBook
            };

            return View(vm);
        }





        [HttpGet]
        public async Task<IActionResult> GetUser()
        {
            var getUser = _user.Users.ToList();

            var userWithRole = new List<UserRoleViewModel>();
            foreach (var i in getUser)
            {
                var roles = await _user.GetRolesAsync(i);
                userWithRole.Add(new UserRoleViewModel
                {
                    UserId = i.Id,
                    Email = i.Email,
                    PhoneNumber = i.PhoneNumber,
                    UserRoles = roles.ToList()
                });
            }
            return new JsonResult(userWithRole);
        }

        [HttpGet]
        public async Task<IActionResult> GetRole(string id)
        {
            var user = await _user.FindByIdAsync(id);
            if (user == null) return NotFound();

            var allRoles = _role.Roles.Select(r => r.Name).ToList();
            var userRoles = await _user.GetRolesAsync(user);

            return new JsonResult(new
            {
                userId = user.Id,
                email = user.Email,
                allRoles,
                userRoles
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditRole([FromForm] string userId, [FromForm] List<string> selectedRoles)
        {
            var user = await _user.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var currentRoles = await _user.GetRolesAsync(user);

            var removeResult = await _user.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(new { message = "Failed to remove existing roles." });
            }

            var addResult = await _user.AddToRolesAsync(user, selectedRoles ?? new List<string>());
            if (!addResult.Succeeded)
            {
                return BadRequest(new { message = "Failed to add new roles." });
            }

            return Ok(new { message = "Role updated successfully." });
        }
        [HttpPost]
        public IActionResult Delete(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            _context.Users.Remove(user);
            _context.SaveChanges();
            return Json(new { success = true, message = "User deleted successfully." });

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = _context.BookLoan
                .Include(bl => bl.Product)
                .FirstOrDefault(bl => bl.BookLoanId == id);

            if (request == null || request.Status != LoanStatus.Pending)
            {
                return NotFound();
            }

            var user = await _user.FindByIdAsync(request.UserId);

            if (user.IsBlockedFromBorrowing)
            {
                TempData["Error"] = $"User is blocked from borrowing due to unpaid fines.";
                return RedirectToAction("Dashboard", "Admin", new { area = "AdminArea" });
            }

            // Set BorrowDate to the current date upon approval
            request.BorrowDate = DateTime.Now;

            // Determine membership and due date
            bool isPremiumMember = user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now;
            int loanDays = isPremiumMember ? 14 : 7;
            request.DueDate = request.BorrowDate.AddDays(loanDays);

            request.Status = LoanStatus.Approved;
            if (request.Product.ProductQuantity > 0)
            {
                request.Product.ProductQuantity -= 1;
            }

            _context.SaveChanges();

            // Prepare membership badge and borrow period description
            string userTypeText = isPremiumMember
                ? "<span style='color:#856404;background:#ffeeba;border-radius:3px;padding:2px 7px;font-weight:bold;'>Premium Member</span>"
                : "<span style='color:#444;background:#e9ecef;border-radius:3px;padding:2px 7px;font-weight:bold;'>General Member</span>";

            string durationText = isPremiumMember
                ? "As a <strong>Premium Member</strong>, your borrow period is <strong>14 days</strong> from the approval date."
                : "As a <strong>General Member</strong>, your borrow period is <strong>7 days</strong> from the approval date.";

            // Professional, well-structured HTML email
            string subject = "Library Borrow Request Approved";
            string body = $@"
    <div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;padding:24px;background:#f7fbff;border-radius:7px'>
        <h2 style='color:#2266aa'>Borrow Request Approved</h2>
        <p>
            Dear <strong>{user.UserName}</strong> {userTypeText},
        </p>
        <p>
            Your request to borrow <strong>{request.Product.ProductName}</strong> has been <span style='color:#198754;font-weight:bold;'>approved</span>.
        </p>
        <p>{durationText}</p>
        <p>
            Please pick up your book from the library or access it through your account.
        </p>
        <table style='margin-top:16px;background:#fff;border-radius:5px;border:1px solid #e3e5ea;width:95%;'>
            <tr>
                <td style='padding:7px;border-bottom:1px solid #eee;font-weight:bold;'>Book Name:</td>
                <td style='padding:7px;border-bottom:1px solid #eee;'>{request.Product.ProductName}</td>
            </tr>
            <tr>
                <td style='padding:7px;font-weight:bold;'>Author:</td>
                <td style='padding:7px;'>{request.Product.ProductAuthor}</td>
            </tr>
            <tr>
                <td style='padding:7px;font-weight:bold;'>Borrow Date:</td>
                <td style='padding:7px;'>{request.BorrowDate:dd MMM yyyy}</td>
            </tr>
            <tr>
                <td style='padding:7px;font-weight:bold;'>Due Date:</td>
                <td style='padding:7px;'>{request.DueDate:dd MMM yyyy}</td>
            </tr>
            <tr>
                <td style='padding:7px;font-weight:bold;'>Membership:</td>
                <td style='padding:7px;'>{(isPremiumMember ? "Premium" : "General")}</td>
            </tr>
            <tr>
                <td style='padding:7px;font-weight:bold;'>Borrow Period:</td>
                <td style='padding:7px;'>{loanDays} days</td>
            </tr>
        </table>
        <p style='margin-top:20px;color:#555;font-size:1.1rem'>
            If you have questions, please contact the library team.
        </p>
        <hr />
        <small style='color:#888;'>Library Management System, IUBAT</small>
    </div>
    ";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            TempData["Success"] = "Request approved and notification sent.";
            return RedirectToAction("Dashboard", "Admin", new { area = "AdminArea" });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectRequest(int id)
        {
            var request = _context.BookLoan.FirstOrDefault(bl => bl.BookLoanId == id);

            if (request == null || request.Status != LoanStatus.Pending)
            {
                return NotFound();
            }

            request.Status = LoanStatus.Rejected;

            _context.SaveChanges();
            TempData["Warning"] = "Request rejected.";
            return RedirectToAction("Dashboard", "Admin", new { area = "AdminArea" });
        }

        [HttpGet]
        public IActionResult ReturnRequest()
        {
            const double fineRatePerDay = 1.0; // Fine rate: $1 per overdue day

            var requests = _context.BookLoan
                .Where(u => u.Status == LoanStatus.ReturnPending)
                .Include(u => u.Product)
                .Include(u => u.User)
                .ToList();

            var viewModel = requests.Select(u =>
            {
                double fineRatePerDay = 50.0;
                if (u.User != null && u.User.IsSubscribed && u.User.SubscriptionEndDate > DateTime.Now)
                {
                    fineRatePerDay = 25.0;
                }
                double fine = 0;
                if (u.DueDate < DateTime.Now)
                {
                    var overdueDays = (DateTime.Now - u.DueDate).Days;
                    if (overdueDays > 0)
                    {
                        fine = overdueDays * fineRatePerDay;
                    }
                }
                return new ReturnRequestViewModel
                {
                    BookLoanId = u.BookLoanId,
                    ProductName = u.Product.ProductName,
                    UserName = u.User.UserName,
                    BorrowDate = u.BorrowDate,
                    DueDate = u.DueDate,
                    FineAmount = fine
                };
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveReturn(int? id)
        {
            var loan = _context.BookLoan.FirstOrDefault(u => u.BookLoanId == id && u.Status == LoanStatus.ReturnPending);
            if (loan == null)
            {
                return NotFound();
            }

            double fineRatePerDay = 50.0;
            var user = _context.Users.FirstOrDefault(u => u.Id == loan.UserId);
            if (user != null && user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now)
            {
                fineRatePerDay = 25.0;
            }
            double finalFine = 0;
            if (loan.DueDate < DateTime.Now)
            {
                var overdueDays = (DateTime.Now - loan.DueDate).Days;
                if (overdueDays > 0)
                {
                    finalFine = overdueDays * fineRatePerDay;
                }
            }

            loan.Status = LoanStatus.Returned;
            loan.ReturnDate = DateTime.Now;
            loan.FineAmount = finalFine;

            var product = _context.Product.FirstOrDefault(u => u.ProductId == loan.ProductId);
            if (product != null)
            {
                product.ProductQuantity += 1;
            }

            _context.SaveChanges();
            TempData["Success"] = "Book return approved.";
            return RedirectToAction("ReturnRequest");
        }

        [HttpGet]
        public IActionResult OverdueList()
        {
            var today = DateTime.Today;
            var overdueLoans = _context.BookLoan
                .Include(b => b.User)
                .Include(b => b.Product)
                .Where(b => b.Status == LoanStatus.Approved && b.DueDate < today)
                .ToList();

            var usersOverdue = overdueLoans
                .GroupBy(loan => loan.User.Id)
                .Select(g =>
                {
                    var user = g.First().User;
                    bool isPremiumMember = user != null && user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now;
                    double fineRatePerDay = isPremiumMember ? 25.0 : 50.0;

                    var fines = g.Select(loan =>
                    {
                        if (loan.FineAmount > 0)
                            return loan.FineAmount;
                        else
                            return (DateTime.Today - loan.DueDate).Days * fineRatePerDay;
                    });

                    return new OverdueUserViewModel
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        OverdueBooks = g.Count(),
                        TotalFine = fines.Sum(),
                        Loans = g.Select(x => x).ToList(),
                        IsPremiumMember = isPremiumMember
                    };
                })
                .OrderByDescending(x => x.TotalFine)
                .ToList();



            return View(usersOverdue);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveAllReturns(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Warning"] = "No pending return requests to approve.";
                return RedirectToAction("ReturnRequest");
            }

            var loans = _context.BookLoan
                .Where(u => ids.Contains(u.BookLoanId) && u.Status == LoanStatus.ReturnPending)
                .ToList();

            foreach (var loan in loans)
            {
                double fineRatePerDay = 50.0;
                var user = _context.Users.FirstOrDefault(u => u.Id == loan.UserId);
                if (user != null && user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now)
                    fineRatePerDay = 25.0;

                double finalFine = 0;
                if (loan.DueDate < DateTime.Now)
                {
                    var overdueDays = (DateTime.Now - loan.DueDate).Days;
                    if (overdueDays > 0)
                        finalFine = overdueDays * fineRatePerDay;
                }

                loan.Status = LoanStatus.Returned;
                loan.ReturnDate = DateTime.Now;
                loan.FineAmount = finalFine;

                var product = _context.Product.FirstOrDefault(u => u.ProductId == loan.ProductId);
                if (product != null)
                    product.ProductQuantity += 1;
            }
            _context.SaveChanges();
            TempData["Success"] = "All pending return requests have been approved.";
            return RedirectToAction("ReturnRequest");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotifyHighFine(string userId)
        {
            var user = await _user.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var today = DateTime.Today;

            var overdueLoans = _context.BookLoan
                .Where(b => b.UserId == userId && b.Status == LoanStatus.Approved && b.DueDate < today)
                .ToList();

            bool isPremium = user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now;
            double fineRate = isPremium ? 25.0 : 50.0;

            double totalFine = overdueLoans
                .Sum(b => b.FineAmount > 0 ? b.FineAmount : (DateTime.Today - b.DueDate).Days * fineRate);

            if (totalFine < 500)
            {
                TempData["Info"] = "This user's fine is less than 500 TK, no warning sent.";
                return RedirectToAction("OverdueList");
            }

            // UPDATED: Email body is more generic
            string subject = "Urgent: Library Fine Notice";
            string body = $@"
        <p>Dear {user.UserName},</p>
        <p>This is a notification that your outstanding library fine has reached <b>৳{totalFine:0.##}</b>.</p>
        <p>Please clear your dues at your earliest convenience to continue using the library services without interruption.</p>
        <p>If you have already cleared this fine, please disregard this message.</p>
        <p>Thank you,<br/>Library Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);
            TempData["Success"] = $"Warning email sent to {user.Email} (fine: ৳{totalFine:0.##})";
            return RedirectToAction("OverdueList");
        }


        [HttpGet]
        public IActionResult AllBorrowedBooks(string filter = "all", string userEmail = "")
        {
            var today = DateTime.Today;

            // For email dropdown
            var userEmails = _context.Users
                .OrderBy(u => u.Email)
                .Select(u => u.Email)
                .ToList();

            var loans = _context.BookLoan
                .Include(b => b.Product)
                .Include(b => b.User)
                .OrderByDescending(b => b.BorrowDate)
                .ToList();

            var viewModel = loans.Select(loan =>
            {
                int overdueDays = 0;
                double fine = 0;
                bool isOverdue = false;

                bool isPremiumMember = loan.User != null && loan.User.IsSubscribed && loan.User.SubscriptionEndDate > DateTime.Now;

                double fineRatePerDay = isPremiumMember ? 25.0 : 50.0;

                if (loan.ReturnDate == null && loan.DueDate < today)
                {
                    overdueDays = (today - loan.DueDate).Days;
                    if (overdueDays > 0)
                    {
                        fine = overdueDays * fineRatePerDay;
                        isOverdue = true;
                    }
                }
                else if (loan.FineAmount > 0)
                {
                    // If fine was set explicitly, use the stored rate if possible.
                    fine = loan.FineAmount;
                    isOverdue = true;
                }

                return new BookLoanWithFineViewModel
                {
                    BookName = loan.Product?.ProductName ?? "",
                    BookAuthor = loan.Product?.ProductAuthor ?? "",
                    UserName = loan.User?.UserName ?? "",
                    Email = loan.User?.Email ?? "",
                    BorrowDate = loan.BorrowDate,
                    DueDate = loan.DueDate,
                    ReturnDate = loan.ReturnDate,
                    FineAmount = fine,
                    IsOverdue = isOverdue,
                    IsPremiumMember = loan.User != null && loan.User.IsSubscribed && loan.User.SubscriptionEndDate > DateTime.Now,
                    BookImageUrl = loan.Product?.ProductImage ?? "/images/default-book.png",


                };
            });

            // Apply filter
            switch (filter)
            {
                case "returned":
                    viewModel = viewModel.Where(x => x.ReturnDate.HasValue);
                    break;
                case "notreturned":
                    viewModel = viewModel.Where(x => x.ReturnDate == null);
                    break;
                case "withfine":
                    viewModel = viewModel.Where(x => x.FineAmount > 0);
                    break;
                case "withoutfine":
                    viewModel = viewModel.Where(x => x.FineAmount <= 0);
                    break;
            }
            // Email filter
            if (!string.IsNullOrEmpty(userEmail))
                viewModel = viewModel.Where(x => x.Email == userEmail);

            ViewBag.UserEmails = userEmails;
            ViewBag.CurrentFilter = filter;
            ViewBag.SelectedEmail = userEmail;
            return View(viewModel.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _user.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // UPDATED: Simply set the boolean flag
            user.IsBlockedFromBorrowing = true;
            await _user.UpdateAsync(user);

            var today = DateTime.Today;
            var overdueLoans = _context.BookLoan
                .Where(b => b.UserId == userId && b.Status == LoanStatus.Approved && b.DueDate < today)
                .ToList();

            bool isPremium = user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now;
            double fineRate = isPremium ? 25.0 : 50.0;
            double totalFine = overdueLoans
                .Sum(b => b.FineAmount > 0 ? b.FineAmount : (DateTime.Today - b.DueDate).Days * fineRate);

            // UPDATED: Email content is more generic
            string subject = "Action Required: Your Library Account has been Blocked";
            string body = $@"
<p>Dear {user.UserName},</p>
<p>Due to outstanding fines amounting to <b>৳{totalFine:0.##}</b>, your library account has been <b>BLOCKED</b>.</p>
<p>You will be restricted from borrowing any new books until all fines are cleared. Once payment is complete, your account will be reactivated automatically.</p>
<p>Please pay your fines to restore borrowing privileges.</p>
<p>Thank you,<br/>Library Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);
            TempData["Success"] = $"User {user.UserName} is now blocked and a notification email has been sent.";

            return RedirectToAction("OverdueList");
        }

        // Unblock after payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _user.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // UPDATED: Simplified unblocking
            user.IsBlockedFromBorrowing = false;
            await _user.UpdateAsync(user);

            TempData["Success"] = $"User {user.UserName} has been unblocked. They can borrow books again.";
            return RedirectToAction("OverdueList");
        }
        [HttpGet]
        public IActionResult PremiumMembers()
        {
            // Only show users who have ever paid for premium!
            var premiums = _context.Users
                .Where(u => u.HasEverSubscribed)
                .OrderByDescending(u => u.SubscriptionEndDate)
                .ToList();
            return View(premiums);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMembership(string userId)
        {
            var user = await _user.FindByIdAsync(userId);
            bool isNowPremium = false;

            if (user != null)
            {
                if (user.IsSubscribed && user.SubscriptionEndDate > DateTime.Now)
                {
                    // Cancel Membership
                    user.IsSubscribed = false;
                    user.SubscriptionEndDate = null;
                    isNowPremium = false;
                }
                else
                {
                    // Re-issue Membership
                    user.IsSubscribed = true;
                    user.SubscriptionEndDate = DateTime.Now.AddMonths(1);
                    user.HasEverSubscribed = true; // Mark as ever-premium
                    isNowPremium = true;
                }
                await _user.UpdateAsync(user);
                return Json(new
                {
                    success = true,
                    isPremium = isNowPremium,
                    endDate = user.SubscriptionEndDate?.ToString("dd MMM yyyy") ?? ""
                });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearAllBorrowedBooks()
        {
            var allBookLoans = _context.BookLoan.ToList();
            if (!allBookLoans.Any())
            {
                TempData["Warning"] = "No borrowed book records to clear.";
                return RedirectToAction("AllBorrowedBooks");
            }

            // Optional: Also remove related Payment records if you want a full clean (be careful!)
            var paymentIds = allBookLoans
                .Select(bl => bl.BookLoanId)
                .ToList();
            var payments = _context.Payment.Where(p => paymentIds.Contains(p.BookLoanId ?? 0)).ToList();
            if (payments.Any())
                _context.Payment.RemoveRange(payments);

            _context.BookLoan.RemoveRange(allBookLoans);
            _context.SaveChanges();

            TempData["Success"] = "All borrowed book records cleared successfully.";
            return RedirectToAction("AllBorrowedBooks");
        }

    }

}
