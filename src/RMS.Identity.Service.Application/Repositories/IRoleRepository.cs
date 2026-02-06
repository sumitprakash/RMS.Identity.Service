namespace RMS.Identity.Service.Application.Repositories
{
    public interface IRoleRepository
    {
        Task<Domain.Entities.Role?> GetByNameAsync(string name);
        Task<long> CreateAsync(Domain.Entities.Role role);
        Task<Domain.Entities.Role?> GetByIdAsync(long roleId);
    }
}
