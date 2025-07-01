using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;


namespace AdminLTE.Models

{
    public class ApplicationUser:IdentityUser
    {
        [Required(ErrorMessage = "Full Name is required")]
        public string? Name { get; set; }
        public string? Image { get; set; }
        public string? Address { get; set; }
        public string? Hobby { get; set; }
        public DateTime? DOB { get; set; }
        public string? Gender { get; set; }

    }
}
