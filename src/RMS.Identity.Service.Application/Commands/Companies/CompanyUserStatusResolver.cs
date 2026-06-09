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

        return user.EmailVerified ? "active" : "pending";
    }
}
