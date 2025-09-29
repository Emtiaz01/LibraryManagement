using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagementSystem.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public PaymentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> FinePayment(int bookLoanId)
        {
            var user = await _userManager.GetUserAsync(User);
            var loan = _context.BookLoan.FirstOrDefault(b => b.BookLoanId == bookLoanId && b.UserId == user.Id);

            if (loan == null) return NotFound();

            var alreadyPaid = _context.Payment.Any(p => p.BookLoanId == bookLoanId && p.Status == "Paid");
            if (alreadyPaid)
                return RedirectToAction("FineAlreadyPaid");

            ViewBag.PublishableKey = _config["Stripe:PublishableKey"];
            ViewBag.Amount = loan.FineAmount;
            ViewBag.BookLoanId = loan.BookLoanId;
            ViewBag.LoanInfo = $"{loan.Product?.ProductName ?? "Book"} | Due: {loan.DueDate:dd MMM yyyy}";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FinePaymentCharge(string stripeToken, double amount, int bookLoanId)
        {
            var user = await _userManager.GetUserAsync(User);

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
            var options = new ChargeCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = "usd",
                Description = $"Library Fine Payment (User: {user.Email}, BookLoanId: {bookLoanId})",
                Source = stripeToken,
            };
            var service = new ChargeService();
            var charge = service.Create(options);

            var payment = new Payment
            {
                UserId = user.Id,
                BookLoanId = bookLoanId,
                Amount = amount,
                Currency = options.Currency,
                PaymentType = "Fine",
                Status = charge.Status == "succeeded" ? "Paid" : "Failed",
                TransactionId = charge.Id,
                PaymentDate = DateTime.UtcNow
            };
            _context.Payment.Add(payment);
            await _context.SaveChangesAsync();

            if (charge.Status == "succeeded")
                return RedirectToAction("FinePaymentSuccess");
            else
                return RedirectToAction("FinePaymentFail");
        }

        public IActionResult FinePaymentSuccess() => View();
        public IActionResult FinePaymentFail() => View();
        public IActionResult FineAlreadyPaid() => View();

        [HttpGet]
        public async Task<IActionResult> MyPayments()
        {
            var user = await _userManager.GetUserAsync(User);
            var payments = _context.Payment
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();
            return View(payments);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AllPayments()
        {
            var payments = _context.Payment
                .Include(p => p.User)
                .Include(p => p.BookLoan)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();
            return View(payments); // finds Areas/AdminArea/Views/Payment/AllPayments.cshtml
        }
    }
}
