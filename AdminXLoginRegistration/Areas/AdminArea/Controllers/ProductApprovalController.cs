using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LibraryManagementSystem.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    public class ProductApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ProductApprovalController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        public IActionResult DonatedBooks()
        {
            var books = _context.Product
                .Include(p => p.Category)
                .Where(p => p.IsDonated && p.DonationStatus == "Approved")
                .OrderByDescending(p => p.ProductId)
                .ToList();
            return View(books);
        }

        public IActionResult PendingDonations()
        {
            var list = _context.Product.Where(p => p.IsDonated && p.DonationStatus == "Pending").ToList();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveDonation(int id)
        {
            var product = _context.Product.Find(id);
            if (product != null)
            {
                product.DonationStatus = "Approved";
                _context.SaveChanges();

                // Send email if donor email exists
                if (!string.IsNullOrWhiteSpace(product.DonorEmail))
                {
                    string subject = "Your Donated Book Was Approved";
                    string body = $"<p>Dear {product.DonorName ?? "Donor"},<br/>" +
                                  $"Your donation of the book <b>{product.ProductName}</b> has been <b>approved</b> and is now part of our library collection.<br/><br/>Thank you for your generous contribution!<br/>- The Library Team</p>";
                    await _emailService.SendEmailAsync(product.DonorEmail, subject, body);
                }
            }
            return RedirectToAction("PendingDonations");
        }

        [HttpPost]
        public async Task<IActionResult> RejectDonation(int id)
        {
            var product = _context.Product.Find(id);
            if (product != null)
            {
                product.DonationStatus = "Rejected";
                _context.SaveChanges();

                // Send email if donor email exists
                if (!string.IsNullOrWhiteSpace(product.DonorEmail))
                {
                    string subject = "Your Donated Book Was Not Approved";
                    string body = $"<p>Dear {product.DonorName ?? "Donor"},<br/>" +
                                  $"Your donation of the book <b>{product.ProductName}</b> was <b>not approved</b> at this time.<br/><br/>Thank you for your willingness to contribute.<br/>- The Library Team</p>";
                    await _emailService.SendEmailAsync(product.DonorEmail, subject, body);
                }
            }
            return RedirectToAction("PendingDonations");
        }
    }
}
