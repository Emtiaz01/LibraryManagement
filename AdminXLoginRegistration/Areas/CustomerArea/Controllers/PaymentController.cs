using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagementSystem.Areas.CustomerArea.Controllers
{
    [Area("CustomerArea")]
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult SubscriptionPayment(string plan)
        {
            int amount = plan == "monthly" ? 100
                       : plan == "twomonth" ? 150
                       : plan == "yearly" ? 700 : 100;
            ViewBag.Plan = plan;
            ViewBag.Amount = amount;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscriptionPayment(string plan, string cardNumber, string expiry, string cvc, string email)
        {
            int amount = plan == "monthly" ? 100
                       : plan == "twomonth" ? 150
                       : plan == "yearly" ? 700 : 100;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                currentUser.IsSubscribed = true;
                currentUser.SubscriptionEndDate =
                    plan == "monthly" ? DateTime.Now.AddMonths(1)
                  : plan == "twomonth" ? DateTime.Now.AddMonths(2)
                  : plan == "yearly" ? DateTime.Now.AddYears(1)
                  : DateTime.Now.AddMonths(1);

                _context.Payment.Add(new Payment
                {
                    UserId = currentUser.Id,
                    Amount = amount,
                    Currency = "bdt",
                    PaymentType = "Membership",
                    Status = "Paid",
                    TransactionId = "SUB-" + Guid.NewGuid().ToString().Substring(0, 8),
                    PaymentDate = DateTime.UtcNow
                });

                _context.Users.Update(currentUser);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("SubscriptionPaymentSuccess");
        }
        public IActionResult SubscriptionPaymentSuccess()
        {
            return View();
        }




        [HttpGet]
        public IActionResult FinePayment(int bookLoanId)
        {
            var userId = _userManager.GetUserId(User);
            var loan = _context.BookLoan
                .Include(x => x.Product)
                .Include(x => x.User) // Make sure to include the user for premium check!
                .FirstOrDefault(b => b.BookLoanId == bookLoanId && b.UserId == userId);

            if (loan == null)
                return NotFound();

            var alreadyPaid = _context.Payment.Any(p => p.BookLoanId == bookLoanId && p.Status == "Paid");
            if (alreadyPaid)
                return RedirectToAction("FineAlreadyPaid");

            double fine = loan.FineAmount;
            if ((loan.ReturnDate == null) && (loan.DueDate < DateTime.Now))
            {
                var overdueDays = (DateTime.Now - loan.DueDate).Days;
                if (overdueDays > 0)
                {
                    double fineRate = 50.0;
                    if (loan.User != null && loan.User.IsSubscribed && loan.User.SubscriptionEndDate > DateTime.Now)
                    {
                        fineRate = 25.0;
                    }
                    fine = overdueDays * fineRate;
                }
            }

            ViewBag.Amount = fine;
            ViewBag.BookLoanId = loan.BookLoanId;
            ViewBag.LoanInfo = $"{loan.Product?.ProductName ?? "Book"} | Due: {loan.DueDate:dd MMM yyyy}";

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> MockFinePayment(decimal amount, int bookLoanId)
        {
            var fakeTxId = "MOCK-TXN-" + Guid.NewGuid().ToString().Substring(0, 8);
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Challenge();

            var payment = new Payment
            {
                UserId = userId,
                BookLoanId = bookLoanId,
                Amount = (double)amount,
                Currency = "bdt",
                PaymentType = "Fine",
                Status = "Paid",
                TransactionId = fakeTxId,
                PaymentDate = DateTime.UtcNow
            };
            _context.Payment.Add(payment);

            var loan = _context.BookLoan.FirstOrDefault(bl => bl.BookLoanId == bookLoanId);
            if (loan != null && loan.ReturnDate == null)
            {
                loan.Status = LoanStatus.ReturnPending;
            }
            await _context.SaveChangesAsync();


            // --- UPDATED Automatic Unblock Logic ---
            // After paying, check if there are any OTHER unpaid fines for overdue books.
            var remainingUnpaidFines = _context.BookLoan
                .Any(b => b.UserId == userId &&
                          b.ReturnDate == null &&
                          b.DueDate < DateTime.Now &&
                          b.FineAmount > 0 &&
                          !_context.Payment.Any(p => p.BookLoanId == b.BookLoanId && p.Status == "Paid"));

            // If no unpaid fines are left, unblock the user.
            if (!remainingUnpaidFines)
            {
                user.IsBlockedFromBorrowing = false;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            // --- End of Unblock Logic ---


            return RedirectToAction("FinePaymentSuccess", new { paymentId = payment.PaymentId });
        }


        public IActionResult FinePaymentSuccess(int paymentId)
        {
            var payment = _context.Payment
                .Include(p => p.BookLoan).ThenInclude(bl => bl.Product)
                .Include(p => p.User)
                .FirstOrDefault(p => p.PaymentId == paymentId);
            if (payment == null)
                return NotFound();
            return View(payment);
        }


        public IActionResult FinePaymentFail()
        {
            return View();
        }

        public IActionResult FineAlreadyPaid()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyPayments()
        {
            var user = await _userManager.GetUserAsync(User);
            var payments = _context.Payment
                .Include(p => p.BookLoan)
                .OrderByDescending(p => p.PaymentDate)
                .Where(p => p.UserId == user.Id)
                .ToList();
            return View(payments);
        }
    }
}
