using AdminLTE.Data;
using AdminLTE.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        // -------------------- INDEX --------------------
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.Id) // sort new records first
                .ToListAsync();

            return View(users);
        }

        // -------------------- Create (Get) --------------------
        [HttpGet]
        public IActionResult Create()
        {
            var model = new ApplicationUser(); // ✅ Must not be null
            return View(model);
        }

        // -------------------- Create (Post) --------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, IFormFile ImageFile)
        {
            // 1. Read password from Request
            var password = Request.Form["Password"];
            if (string.IsNullOrWhiteSpace(model.Name) ||
               string.IsNullOrWhiteSpace(model.UserName) ||
               string.IsNullOrWhiteSpace(model.Email) ||
               string.IsNullOrWhiteSpace(password))
            {
                // Show general error
                ViewBag.RequiredError = "Fields marked with * are required. Please fill them.";
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.PhoneNumber, @"^\d{10}$"))
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number must be exactly 10 digits.");
                    return View(model); // Return early if phone is invalid
                }
            }

            // 3. Process image upload
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

            // 4. Read Hobbies from form
            var selectedHobbies = Request.Form["Input.Hobby"];
            model.Hobby = string.Join(", ", selectedHobbies);

            // 5. Create user with password
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

        // -------------------- EDIT (GET) --------------------
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user); // ✅ Pass fresh user data (with updated Image)
        }

        // -------------------- EDIT (POST) --------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser updatedUser, IFormFile ImageFile)
        {
            var user = await _userManager.FindByIdAsync(updatedUser.Id);
            if (user == null)
                return NotFound();

            // ✅ Image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Delete old image
                if (!string.IsNullOrEmpty(user.Image) && user.Image != "default.png")
                {
                    var oldPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", user.Image);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                user.Image = fileName;
            }

            // ✅ Update safe fields
            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.UserName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Address = updatedUser.Address;
            user.DOB = updatedUser.DOB;
            user.Gender = updatedUser.Gender;

            // ✅ Update Hobby from checkbox list
            var selectedHobbies = Request.Form["Input.Hobby"];
            user.Hobby = string.Join(", ", selectedHobbies);

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
        //delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Delete image if not default
            if (!string.IsNullOrEmpty(user.Image) && user.Image != "default.png")
            {
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", user.Image);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction("Index");
        }

    }
}
