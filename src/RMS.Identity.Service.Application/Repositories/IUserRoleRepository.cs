namespace RMS.Identity.Service.Application.Repositories
{
    public interface IUserRoleRepository
    {
        Task AssignRoleAsync(long userId, long roleId, long? assignedBy);
        Task RemoveRoleAsync(long userId, long roleId);
        Task<IEnumerable<RMS.Identity.Service.Domain.Entities.Role>> GetRolesForUserAsync(long userId);
    }
}
