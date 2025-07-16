using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class AdminNotificationViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public AdminNotificationViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var pendingRequests = await _context.BookLoan
            .Include(b => b.Product)
            .Include(b => b.User)
            .Where(b => b.Status == LoanStatus.Pending)
            .OrderByDescending(b => b.BorrowDate)
            .ToListAsync();

        var model = new AdminNotificationViewModel
        {
            PendingCount = pendingRequests.Count,
            PendingRequests = pendingRequests
        };

        return View(model);
    }
}
