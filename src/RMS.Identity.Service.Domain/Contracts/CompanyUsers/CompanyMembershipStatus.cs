namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public enum CompanyMembershipStatus
{
    Active,
    Invited,
    Suspended
}

public static class CompanyMembershipStatusExtensions
{
    public static string ToStorageValue(this CompanyMembershipStatus value) =>
        value switch
        {
            CompanyMembershipStatus.Active => "active",
            CompanyMembershipStatus.Invited => "invited",
            CompanyMembershipStatus.Suspended => "suspended",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
