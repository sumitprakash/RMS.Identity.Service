namespace RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompanyUser;

public sealed class UpdateCompanyUserRequestBody
{
    public string CompanyRole { get; init; } = string.Empty;

    public string MembershipStatus { get; init; } = string.Empty;
}
