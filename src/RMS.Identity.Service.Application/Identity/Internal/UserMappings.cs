using RMS.Identity.Service.Application.Identity.Results;
using RMS.Identity.Service.Domain.Entities;

namespace RMS.Identity.Service.Application.Identity.Internal;

internal static class UserMappings
{
    public static UserResult ToResult(UserAccount user, IReadOnlyList<Role> roles, DateTime nowUtc)
    {
        return new UserResult(
            user.UserUUID,
            user.Username,
            user.DisplayName,
            roles.Select(role => role.Name).ToArray(),
            ResolveStatus(user, nowUtc),
            user.CreatedAt);
    }

    private static string ResolveStatus(UserAccount user, DateTime nowUtc)
    {
        if (!user.EmailVerified)
        {
            return "pending";
        }

        if (!user.IsActive || (user.LockedUntil.HasValue && user.LockedUntil.Value > nowUtc))
        {
            return "suspended";
        }

        return "active";
    }
}
