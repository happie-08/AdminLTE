using AdminLTE.Data;
using AdminLTE.Helpers;
using AdminLTE.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AdminLTE.Controllers
{
    public class ManageUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ManageUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
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
                })
                .ToListAsync();

            return Json(new { data = users });
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.IsEdit = false;
            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name");
            return View(new ApplicationUser());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, IFormFile ImageFile)
        {
            var password = Request.Form["Password"];

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.UserName) ||
                string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.RequiredError = "Fields marked with * are required.";
                ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber) &&
                !System.Text.RegularExpressions.Regex.IsMatch(model.PhoneNumber, @"^\d{10}$"))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number must be exactly 10 digits.");
                ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name");
                return View(model);
            }

            model.Image = await ImageHelper.SaveImageAsync(ImageFile, _env);
            model.Hobby = string.Join(", ", Request.Form["Input.Hobby"]);
            model.RoleId = int.Parse(Request.Form["RoleId"]);

            var result = await _userManager.CreateAsync(model, password);
            if (result.Succeeded)
            {
                TempData["Success"] = "User created successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.IsEdit = true;
            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name", user.RoleId);
            return View("Create",user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser updatedUser, IFormFile ImageFile)
        {
            var user = await _userManager.FindByIdAsync(updatedUser.Id);
            if (user == null) return NotFound();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Delete old image
                ImageHelper.DeleteImage(user.Image, _env);

                // Save new image
                user.Image = await ImageHelper.SaveImageAsync(ImageFile, _env);
            }

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.UserName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Address = updatedUser.Address;
            user.DOB = updatedUser.DOB;
            user.Gender = updatedUser.Gender;
            user.Hobby = string.Join(", ", Request.Form["Input.Hobby"]);
            user.RoleId = int.Parse(Request.Form["RoleId"]);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User updated successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name", user.RoleId);
            return View("Create",updatedUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ImageHelper.DeleteImage(user.Image, _env);
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                TempData["Success"] = "User deleted successfully.";
            else
                TempData["Error"] = "Failed to delete user.";

            return RedirectToAction("Index");
        }
    }
}
