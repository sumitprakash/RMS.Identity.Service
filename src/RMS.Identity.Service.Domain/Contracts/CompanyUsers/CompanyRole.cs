namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public enum CompanyRole
{
    Owner,
    Admin,
    Member
}

public static class CompanyRoleExtensions
{
    public static string ToStorageValue(this CompanyRole value) =>
        value switch
        {
            CompanyRole.Owner => "OWNER",
            CompanyRole.Admin => "ADMIN",
            CompanyRole.Member => "MEMBER",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static CompanyRole FromStorageValue(string? value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "OWNER" => CompanyRole.Owner,
            "ADMIN" => CompanyRole.Admin,
            "MEMBER" => CompanyRole.Member,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown company role.")
        };
}
