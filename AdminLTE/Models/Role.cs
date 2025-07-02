using System.ComponentModel.DataAnnotations;

namespace AdminLTE.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public bool Active { get; set; } = true;
        public ICollection<ApplicationUser>? Users { get; set; }

    }
}
