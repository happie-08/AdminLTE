using AdminLTE.Data;
using AdminLTE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AdminLTE.Controllers
{
    [Authorize]
    public class RoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public RoleController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var role = await _context.Roles.FindAsync(user.RoleId);

            if (role?.Name != "Admin")
            {
                TempData["AccessDenied"] = "❌ Access Denied: Only Admin can access this section.";
                return RedirectToAction("Index", "Home");
            }

            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }

        // ================= CREATE =================
        public IActionResult Create()
        {
            ViewBag.IsEdit = false;
            return View("Create", new Role());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role model)
        {
            ViewBag.IsEdit = false;

            if (!ModelState.IsValid)
                return View("Create", model);

            bool exists = await _context.Roles
                .AnyAsync(r => r.Name.ToLower() == model.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "Role name already exists.");
                return View("Create", model);
            }

            _context.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            ViewBag.IsEdit = true;
            return View("Create", role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role role)
        {
            ViewBag.IsEdit = true;

            if (id != role.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View("Create", role);

            bool duplicate = _context.Roles
                .Any(r => r.Name.ToLower() == role.Name.ToLower() && r.Id != id);

            if (duplicate)
            {
                ModelState.AddModelError("Name", "Another role with the same name already exists.");
                return View("Create", role);
            }

            try
            {
                _context.Update(role);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Roles.Any(r => r.Id == role.Id))
                    return NotFound();
                else
                    throw;
            }
        }

        // ================= DELETE =================
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound();

            bool isRoleInUse = _context.Users.Any(u => u.RoleId == id);
            if (isRoleInUse)
            {
                TempData["Error"] = "❌ Cannot delete this role because it is assigned to one or more users.";
                return RedirectToAction(nameof(Index));
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            TempData["Success"] = "✅ Role deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
