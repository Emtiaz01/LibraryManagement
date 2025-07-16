using AdminXLoginRegistration.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LibraryManagementSystem.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    //[Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _user;
        private readonly RoleManager<IdentityRole> _role;
        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> user, RoleManager<IdentityRole> role)
        {
            _user = user;
            _role = role;
            _context = context;
        }
        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Index()
        { 
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetUser()
        {
            var getUser = _user.Users.ToList();

            var userWithRole = new List<UserRoleViewModel>();
            foreach(var i in getUser)
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
        [ValidateAntiForgeryToken]
        public IActionResult ApproveRequest(int id)
        {
            var request = _context.BookLoan
                .Include(bl => bl.Product)
                .FirstOrDefault(bl => bl.BookLoanId == id);

            if (request == null || request.Status != LoanStatus.Pending)
            {
                return NotFound();
            }

            // Approve the request
            request.Status = LoanStatus.Approved;

            // Reduce stock
            if (request.Product.ProductQuantity > 0)
            {
                request.Product.ProductQuantity -= 1;
            }

            _context.SaveChanges();
            TempData["Success"] = "Request approved.";
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

            // Reject the request
            request.Status = LoanStatus.Rejected;

            _context.SaveChanges();
            TempData["Warning"] = "Request rejected.";
            return RedirectToAction("Dashboard", "Admin", new { area = "AdminArea" });
        }

    }
}
