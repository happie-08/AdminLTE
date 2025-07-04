using AdminLTE.Data;
using AdminLTE.Helpers;
using AdminLTE.Models;
using AdminLTE.Repositories.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AdminLTE.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
            : base(context)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }
        public override async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role) // ✅ Fix: include Role data
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ApplicationUser> FindByIdAsync(string id)
        {
            return await _userManager.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task AddUserAsync(ApplicationUser user, string password, IFormFile image)
        {
            user.Image = await ImageHelper.SaveImageAsync(image, _env);
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new System.Exception(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public async Task UpdateUserAsync(ApplicationUser user, IFormFile image)
        {
            if (image != null)
            {
                ImageHelper.DeleteImage(user.Image, _env);
                user.Image = await ImageHelper.SaveImageAsync(image, _env);
            }
            await _userManager.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(ApplicationUser user)
        {
            ImageHelper.DeleteImage(user.Image, _env);
            await _userManager.DeleteAsync(user);
        }
    }
}
