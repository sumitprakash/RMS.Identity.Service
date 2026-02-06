using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Application.Repositories
{
    public interface IUserRepository
    {
        Task<UserAccount?> GetByUsernameAsync(long companyId, string username);
        Task<UserAccount?> GetByUserUuidAsync(Guid userUuid);
        Task<UserAccount?> GetUserWithRolesAsync(long companyId, string username);
        Task<UserAccount?> GetByIdAsync(long userId);
        Task<long> CreateAsync(UserAccount user);
    }
}
