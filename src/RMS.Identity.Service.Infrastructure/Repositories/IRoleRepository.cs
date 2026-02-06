namespace RMS.Identity.Service.Infrastructure.Repositories
{
    public interface IRoleRepository
    {
        Task<RMS.Identity.Service.Domain.Entities.Role?> GetByNameAsync(string name);
        Task<long> CreateAsync(RMS.Identity.Service.Domain.Entities.Role role);
        Task<RMS.Identity.Service.Domain.Entities.Role?> GetByIdAsync(long roleId);
    }
}
