using AdminLTE.Data;
using AdminLTE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

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

        // ----------- INDEX -------------
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // ----------- CREATE (GET) -------------
        public IActionResult Create()
        {
            return View();
        }

        // ----------- CREATE (POST) -------------
        // ----------- CREATE (POST) -------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                // Debug: Log model state errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(model);
            }

            // Handle image upload (optional)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", fileName);

                // Debug: Check if directory exists
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    Console.WriteLine("Created uploads directory: " + uploadsFolder);
                }

                // Save the file
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                    Console.WriteLine("Image saved to: " + path);
                }
                model.Image = fileName; // Set the filename to the model
            }
            else
            {
                model.Image = "default.png"; // Default image if none uploaded
                Console.WriteLine("No image uploaded, using default: default.png");
            }

            // Create user with Identity
            var result = await _userManager.CreateAsync(model, "DefaultPassword123!"); // Replace with secure password logic
            if (result.Succeeded)
            {
                TempData["Success"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                Console.WriteLine("Identity Error: " + error.Description);
            }
            return View(model);
        }

        // ----------- EDIT (GET) -------------
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // ----------- EDIT (POST) -------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser updatedUser, IFormFile ImageFile)
        {
            var user = await _userManager.FindByIdAsync(updatedUser.Id);
            if (user == null)
                return NotFound();

            // ✅ Handle image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Delete old image if not default
                if (!string.IsNullOrEmpty(user.Image) && user.Image != "default.png")
                {
                    var oldPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", user.Image);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                user.Image = fileName;
                HttpContext.Session.SetString("UserImage", fileName);
            }

            // ✅ Update other fields (no validation)
            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.UserName;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Address = updatedUser.Address;
            user.DOB = updatedUser.DOB;
            user.Gender = updatedUser.Gender;
            user.Hobby = updatedUser.Hobby;

            // ✅ Also store session username
            HttpContext.Session.SetString("UserName", user.UserName ?? "Guest");

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
    }
}