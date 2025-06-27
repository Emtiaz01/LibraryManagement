using LibraryManagementSystem.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Threading.Tasks;

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
            //var userlist = _user.Users.ToList(); 
            return View();
        }

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
                allRoles = allRoles,
                userRoles = userRoles
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
    }
}
