using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

public sealed class RefreshRequestValidator : RequestValidator<RefreshRequest>
{
    public override void Validate(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body.RefreshToken))
        {
            throw ValidationError("Refresh token is required.");
        }

        if (request.Body.RefreshToken.Length > 256)
        {
            throw ValidationError("Refresh token must not exceed 256 characters.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
