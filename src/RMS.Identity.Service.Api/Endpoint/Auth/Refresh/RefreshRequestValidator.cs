using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

public sealed class RefreshRequestValidator
{
    public void Validate(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body.RefreshToken))
        {
            throw ValidationError("Refresh token is required.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new BadRequestException(message);
}
