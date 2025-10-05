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

                // Get current date for approval
                var approvalDate = DateTime.Now.ToString("MMMM dd, yyyy");
                var donationDate = product.DonationDate == default(DateTime)
                    ? "N/A"
                    : product.DonationDate.ToString("MMMM dd, yyyy");

                if (!string.IsNullOrWhiteSpace(product.DonorEmail))
                {
                    string subject = $"Approval Notice: Donated Book '{product.ProductName}'";
                    string body = $@"
                <p>Dear {(product.DonorName ?? "Donor")},</p>

                <p>We are pleased to inform you that your generous donation has been <strong>approved</strong> and added to our library collection. Below are the details:</p>

                <ul>
                    <li><strong>Book Title:</strong> {product.ProductName}</li>
                    <li><strong>Category:</strong> {product.Category?.CategoryName ?? "N/A"}</li>
                    <li><strong>Donation Date:</strong> {donationDate}</li>
                    <li><strong>Approval Date:</strong> {approvalDate}</li>
                </ul>

                <p>We sincerely appreciate your contribution and your support in promoting the joy of reading within our community.</p>

                <p>Thank you once again for your support.</p>

                <p>Best regards,<br/>
                The Library Team</p>
            ";
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

                // Get current date for response
                var responseDate = DateTime.Now.ToString("MMMM dd, yyyy");
                var donationDate = product.DonationDate == default(DateTime)
                    ? "N/A"
                    : product.DonationDate.ToString("MMMM dd, yyyy");

                if (!string.IsNullOrWhiteSpace(product.DonorEmail))
                {
                    string subject = $"Status Update: Donated Book '{product.ProductName}'";
                    string body = $@"
                <p>Dear {(product.DonorName ?? "Donor")},</p>

                <p>Thank you very much for donating your book to our library. While we value every offer, we regret to inform you that your donation has not been approved at this time. Please find the details below:</p>

                <ul>
                    <li><strong>Book Title:</strong> {product.ProductName}</li>
                    <li><strong>Category:</strong> {product.Category?.CategoryName ?? "N/A"}</li>
                    <li><strong>Donation Date:</strong> {donationDate}</li>
                    <li><strong>Decision Date:</strong> {responseDate}</li>
                </ul>

                <p>We greatly appreciate your willingness to contribute and encourage you to reach out with future donations.</p>

                <p>Best regards,<br/>
                The Library Team</p>
            ";
                    await _emailService.SendEmailAsync(product.DonorEmail, subject, body);
                }
            }
            return RedirectToAction("PendingDonations");
        }

    }
}
