using AdminLTE.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AdminLTE.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<ApplicationUser>
    {
        Task<ApplicationUser> FindByIdAsync(string id);
        Task AddUserAsync(ApplicationUser user, string password, IFormFile image);
        Task UpdateUserAsync(ApplicationUser user, IFormFile image);
        Task DeleteUserAsync(ApplicationUser user);
    }
}
