using System.Security.Claims;

namespace RMS.Identity.Service.Api.Extensions;

internal static class ClaimsPrincipalExtensions
{
    public static Guid? GetCompanyUuid(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("companyUuid")?.Value;
        return Guid.TryParse(value, out var companyUuid) ? companyUuid : null;
    }
}
