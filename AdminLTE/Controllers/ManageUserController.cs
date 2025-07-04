using AdminLTE.Helpers;
using AdminLTE.Models;
using AdminLTE.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace AdminLTE.Controllers
{
    public class ManageUserController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IWebHostEnvironment _env; // 👈 Add this field

        public ManageUserController(IUserRepository userRepository, IRoleRepository roleRepository, IWebHostEnvironment env)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = (await _userRepository.GetAllAsync())
                .OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.PhoneNumber,
                    u.UserName,
                    u.Address,
                    DOB = u.DOB.HasValue ? u.DOB.Value.ToString("dd/MM/yyyy") : "-",
                    u.Gender,
                    u.Hobby,
                    RoleName = u.Role != null ? u.Role.Name : "-",
                    Image = string.IsNullOrEmpty(u.Image) ? "default.png" : u.Image
                });

            return Json(new { data = users });
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.IsEdit = false; 
            ViewBag.RoleList = new SelectList(await _roleRepository.GetActiveRolesAsync(), "Id", "Name");
            return View(new ApplicationUser());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, IFormFile? ImageFile)
        {
            ViewBag.IsEdit = false; 

            if (!ModelState.IsValid)
            {
                ViewBag.RoleList = new SelectList(await _roleRepository.GetActiveRolesAsync(), "Id", "Name", user.RoleId);
                return View(user);
            }

            var password = Request.Form["Password"];
            user.Hobby = string.Join(", ", Request.Form["Hobby"]);
            await _userRepository.AddUserAsync(user, password, ImageFile);
            TempData["Success"] = "User created successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.IsEdit = true; // 👈 Important
            ViewBag.RoleList = new SelectList(await _roleRepository.GetActiveRolesAsync(), "Id", "Name", user.RoleId);
            return View("Create", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser updatedUser, IFormFile? ImageFile)
        {
            ViewBag.IsEdit = true;

            if (!ModelState.IsValid)
            {
                ViewBag.RoleList = new SelectList(await _roleRepository.GetActiveRolesAsync(), "Id", "Name", updatedUser.RoleId);
                return View("Create", updatedUser);
            }

            // ✅ Load original user from DB (tracked by EF)
            var existingUser = await _userRepository.FindByIdAsync(updatedUser.Id);
            if (existingUser == null) return NotFound();

            // ✅ Update only changed fields
            existingUser.Name = updatedUser.Name;
            existingUser.Email = updatedUser.Email;
            existingUser.UserName = updatedUser.UserName;
            existingUser.PhoneNumber = updatedUser.PhoneNumber;
            existingUser.Address = updatedUser.Address;
            existingUser.DOB = updatedUser.DOB;
            existingUser.Gender = updatedUser.Gender;
            // Combine all selected hobbies into a comma-separated string
            existingUser.Hobby = string.Join(", ", Request.Form["Hobby"]);
            existingUser.RoleId = updatedUser.RoleId;

            // ✅ Handle Image
            if (ImageFile != null && ImageFile.Length > 0)
            {
                ImageHelper.DeleteImage(existingUser.Image, _env);
                existingUser.Image = await ImageHelper.SaveImageAsync(ImageFile, _env);
            }

            // ✅ Pass updated tracked object
            await _userRepository.UpdateUserAsync(existingUser, null);

            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userRepository.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userRepository.DeleteUserAsync(user);
            TempData["Success"] = "User deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
