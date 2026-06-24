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
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
