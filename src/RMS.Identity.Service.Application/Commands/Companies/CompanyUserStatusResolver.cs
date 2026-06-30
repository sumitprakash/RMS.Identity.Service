using RMS.Identity.Service.Domain.Entities.Companies;

namespace RMS.Identity.Service.Application.Commands.Companies;

internal static class CompanyUserStatusResolver
{
    public static string Resolve(CompanyUserAccount user)
    {
        if (!user.IsActive || string.Equals(user.MembershipStatus, "suspended", StringComparison.OrdinalIgnoreCase))
        {
            return "suspended";
        }

        if (!string.Equals(user.MembershipStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return "pending";
        }

        return user.EmailVerified ? "active" : "pending";
    }
}
