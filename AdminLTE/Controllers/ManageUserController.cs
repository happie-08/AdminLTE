using AdminLTE.Data;
using AdminLTE.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AdminLTE.Controllers
{
    public class ManageUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ManageUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostingEnvironment = hostingEnvironment;
        }

        //  Get API (for DataTable)
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
            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name");
            return View(new ApplicationUser());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, IFormFile ImageFile)
        {
            var password = Request.Form["Password"];
            if (string.IsNullOrWhiteSpace(model.Name) ||
               string.IsNullOrWhiteSpace(model.UserName) ||
               string.IsNullOrWhiteSpace(model.Email) ||
               string.IsNullOrWhiteSpace(password))
            {
                ViewBag.RequiredError = "Fields marked with * are required.";
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber) &&
                !System.Text.RegularExpressions.Regex.IsMatch(model.PhoneNumber, @"^\d{10}$"))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number must be exactly 10 digits.");
                return View(model);
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                model.Image = fileName;
            }
            else
            {
                model.Image = "default.png";
            }

            var selectedHobbies = Request.Form["Input.Hobby"];
            model.Hobby = string.Join(", ", selectedHobbies);
            model.RoleId = int.Parse(Request.Form["RoleId"]);

            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name");

            var result = await _userManager.CreateAsync(model, password);
            if (result.Succeeded)
            {
                TempData["Success"] = "User created successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name", user.RoleId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser updatedUser, IFormFile ImageFile)
        {
            var user = await _userManager.FindByIdAsync(updatedUser.Id);
            if (user == null) return NotFound();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(user.Image) && user.Image != "default.png")
                {
                    var oldPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", user.Image);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                user.Image = fileName;
            }

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.UserName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Address = updatedUser.Address;
            user.DOB = updatedUser.DOB;
            user.Gender = updatedUser.Gender;

            var selectedHobbies = Request.Form["Input.Hobby"];
            user.Hobby = string.Join(", ", selectedHobbies);
            user.RoleId = int.Parse(Request.Form["RoleId"]);

            ViewBag.RoleList = new SelectList(_context.Roles.Where(r => r.Active), "Id", "Name", user.RoleId);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User updated successfully!";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(updatedUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(user.Image) && user.Image != "default.png")
            {
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", user.Image);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                TempData["Success"] = "User deleted successfully.";
            else
                TempData["Error"] = "Failed to delete user.";

            return RedirectToAction("Index");
        }
    }
}
