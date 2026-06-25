using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Companies.CreateCompanyUser;

public sealed class CreateCompanyUserRequestValidator : RequestValidator<CreateCompanyUserRequest>
{
    private static readonly string[] AllowedCompanyRoles = ["OWNER", "ADMIN", "MEMBER"];

    public override void Validate(CreateCompanyUserRequest request)
    {
        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body.Username))
        {
            throw ValidationError("Username is required.");
        }

        if (!EmailAddressValidator.IsValid(body.Username))
        {
            throw ValidationError("Username must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(body.CompanyRole))
        {
            throw ValidationError("Company role is required.");
        }

        if (!AllowedCompanyRoles.Contains(body.CompanyRole.Trim().ToUpperInvariant(), StringComparer.Ordinal))
        {
            throw ValidationError("Company role must be OWNER, ADMIN, or MEMBER.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
