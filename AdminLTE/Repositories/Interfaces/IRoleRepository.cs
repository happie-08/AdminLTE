using AdminLTE.Models;

namespace AdminLTE.Repositories.Interfaces
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<IEnumerable<Role>> GetActiveRolesAsync();
    }
}
