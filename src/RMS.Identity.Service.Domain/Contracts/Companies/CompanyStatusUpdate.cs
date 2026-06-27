namespace RMS.Identity.Service.Domain.Contracts.Companies;

public enum CompanyStatusUpdate
{
    Verified,
    Rejected,
    Suspended
}

public static class CompanyStatusUpdateExtensions
{
    public static string ToStorageValue(this CompanyStatusUpdate value) =>
        value switch
        {
            CompanyStatusUpdate.Verified => "verified",
            CompanyStatusUpdate.Rejected => "rejected",
            CompanyStatusUpdate.Suspended => "suspended",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
