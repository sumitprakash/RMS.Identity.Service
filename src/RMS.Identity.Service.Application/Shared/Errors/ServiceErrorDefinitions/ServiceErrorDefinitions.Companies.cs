namespace RMS.Identity.Service.Application.Shared.Errors;

public static partial class ServiceErrorDefinitions
{
    public static class Companies
    {
        public static readonly ServiceError CompanyNotFound = new(
            new ServiceErrorCode(4, 1),
            "Company could not be found.");

        public static readonly ServiceError CompanyExists = new(
            new ServiceErrorCode(4, 2),
            "Company GSTIN already exists.");

        public static readonly ServiceError InvalidCompanyStatusTransition = new(
            new ServiceErrorCode(4, 3),
            "Company status transition is not allowed.");
    }
}
