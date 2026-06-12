namespace RMS.Identity.Service.Domain.Shared.Errors;

public static partial class ServiceErrors
{
    public static class CompanyUsers
    {
        public static readonly ServiceError CompanyUserNotFound = new(
            new ServiceErrorCode(5, 1),
            "Company user could not be found.");

        public static readonly ServiceError LastOwnerRequired = new(
            new ServiceErrorCode(5, 2),
            "Company must retain at least one active OWNER.");
    }
}
