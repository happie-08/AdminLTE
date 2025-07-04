using AdminLTE.Models;
using AdminLTE.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AdminLTE.Controllers
{
    public class RoleController : Controller
    {
        private readonly IRoleRepository _roleRepository;

        public RoleController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }
        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var roles = (await _roleRepository.GetAllAsync())
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    description = r.Description,
                    active = r.Active
                        ? "<i class='fas fa-check-circle text-success fa-lg'></i>"
                        : "<i class='fas fa-times-circle text-danger fa-lg'></i>"
                });

            return Json(new { data = roles });
        }

        public async Task<IActionResult> Index()
        {
            return View(await _roleRepository.GetAllAsync());
        }

        public IActionResult Create()
        {
            ViewBag.IsEdit = false;
            return View("Create",new Role());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (!ModelState.IsValid)
                return View("create",role);

            await _roleRepository.AddAsync(role);
            await _roleRepository.SaveAsync();

            TempData["Success"] = "Role created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return NotFound();

            ViewBag.IsEdit = true;
            return View("Create",role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Role role)
        {
            if (!ModelState.IsValid)
                return View("Create",role);

            await _roleRepository.UpdateAsync(role);
            await _roleRepository.SaveAsync();

            TempData["Success"] = "Role updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return NotFound();

            await _roleRepository.DeleteAsync(role);
            await _roleRepository.SaveAsync();

            TempData["Success"] = "Role deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
