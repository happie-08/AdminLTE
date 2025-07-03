using AdminLTE.Models;
using AdminLTE.Helpers; // ? Add this for ImageHelper
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AdminLTE.Areas.Identity.Pages.Account
{
    [Authorize]
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public EditProfileModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Full Name is required")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; }

            [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Username is required")]
            public string UserName { get; set; }

            public string Address { get; set; }
            public string Hobby { get; set; }
            public string Gender { get; set; }
            public DateTime? DOB { get; set; }
            public IFormFile? ImageFile { get; set; }
            public string? ImagePath { get; set; }
        }

        [TempData]
        public string StatusMessage { get; set; }


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

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ? Use ImageHelper to Save New Image (and delete old)
            if (Input.ImageFile != null)
            {
                ImageHelper.DeleteImage(user.Image, _env);
                user.Image = await ImageHelper.SaveImageAsync(Input.ImageFile, _env);
                HttpContext.Session.SetString("UserImage", user.Image);
            }

            // ? Update other fields
            user.Name = Input.Name;
            user.PhoneNumber = Input.PhoneNumber;
            user.Address = Input.Address;
            user.Gender = Input.Gender;
            user.DOB = Input.DOB;

            var selectedHobbies = Request.Form["Input.Hobby"];
            user.Hobby = string.Join(", ", selectedHobbies);

            // ? Update Email if changed
            if (user.Email != Input.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(Input.Email);
                if (emailExists != null && emailExists.Id != user.Id)
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

            // ? Update Username if changed
            if (user.UserName != Input.UserName)
            {
                var usernameExists = await _userManager.FindByNameAsync(Input.UserName);
                if (usernameExists != null && usernameExists.Id != user.Id)
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

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.RefreshSignInAsync(user);

                HttpContext.Session.SetString("UserName", user.UserName);

                StatusMessage = "? Profile updated successfully!";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return Page();
        }
    }
}
