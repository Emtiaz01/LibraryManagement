using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _user;
        private readonly RoleManager<IdentityRole> _role;
        public AdminController(UserManager<IdentityUser> user, RoleManager<IdentityRole> role)
        {
            _user = user;
            _role = role;
        }
        public IActionResult Index()
        {
            var userlist = _user.Users.ToList(); 
            return View(userlist);
        }
        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _user.FindByIdAsync(id);
            if (user == null) return NotFound();

            var allRoles = _role.Roles.Select(r => r.Name).ToList();
            var userRoles = await _user.GetRolesAsync(user);

            var model = new EditRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = allRoles,
                UserRoles = userRoles.ToList()
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditRole(string userId, List<string> selectedRoles)
        {
            var user = await _user.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _user.GetRolesAsync(user);

            var removeResult = await _user.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError("", "Failed to remove existing roles.");
                //return View(); 
            }

            // ✅ Add only the selected roles
            var addResult = await _user.AddToRolesAsync(user, selectedRoles ?? new List<string>());
            if (!addResult.Succeeded)
            {
                ModelState.AddModelError("", "Failed to add new roles.");
                //return View(); 
            }

            return RedirectToAction("Index");
        }

    }
}
