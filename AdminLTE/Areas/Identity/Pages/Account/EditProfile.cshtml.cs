using AdminLTE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AdminLTE.Areas.Identity.Pages.Account
{
    [Authorize]
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public EditProfileModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string StatusMessage { get; set; }

        public class InputModel
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string UserName { get; set; }
            public string Address { get; set; }
            public string Hobby { get; set; }
            public string Gender { get; set; }
            public DateTime? DOB { get; set; }
            public IFormFile? ImageFile { get; set; }
            public string ImagePath { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            Input = new InputModel
            {
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                Address = user.Address,
                Hobby = user.Hobby,
                Gender = user.Gender,
                DOB = user.DOB,
                ImagePath = user.Image ?? "default.png"
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            // ? Save image if uploaded
            if (Input.ImageFile != null)
            {
                var uploadsFolder = Path.Combine("wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ImageFile.CopyToAsync(stream);
                }

                user.Image = fileName;
                HttpContext.Session.SetString("UserImage", fileName);
            }

            // ? Safe Email update
            if (user.Email != Input.Email)
            {
                // Check if email is taken
                var existingEmailUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingEmailUser != null && existingEmailUser.Id != user.Id)
                {
                    ModelState.AddModelError("Input.Email", $"The email '{Input.Email}' is already in use.");
                    return Page();
                }

                var emailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!emailResult.Succeeded)
                {
                    foreach (var error in emailResult.Errors)
                        ModelState.AddModelError("Input.Email", error.Description);
                    return Page();
                }
            }


            // ? Safe Username update
            if (user.UserName != Input.UserName)
            {
                // Check if username is taken
                var existingUser = await _userManager.FindByNameAsync(Input.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Input.UserName", $"The username '{Input.UserName}' is already taken.");
                    return Page();
                }

                var usernameResult = await _userManager.SetUserNameAsync(user, Input.UserName);
                if (!usernameResult.Succeeded)
                {
                    foreach (var error in usernameResult.Errors)
                        ModelState.AddModelError("Input.UserName", error.Description);
                    return Page();
                }
            }

            // ? Update remaining properties
            user.Name = Input.Name;
            user.PhoneNumber = Input.PhoneNumber;
            user.Address = Input.Address;
            user.Gender = Input.Gender;
            user.DOB = Input.DOB;

            var selectedHobbies = Request.Form["Input.Hobby"];
            user.Hobby = string.Join(", ", selectedHobbies);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.RefreshSignInAsync(user);

                // ? Update session again after all updates
                HttpContext.Session.SetString("UserName", user.UserName);
                if (!string.IsNullOrEmpty(user.Image))
                    HttpContext.Session.SetString("UserImage", user.Image);

                StatusMessage = "? Profile updated successfully!";
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                Console.WriteLine("Error: " + error.Description); // for debugging
            }
            
            return Page();
        }

    }
}
