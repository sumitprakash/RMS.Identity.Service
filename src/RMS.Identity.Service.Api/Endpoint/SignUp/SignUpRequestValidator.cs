using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed class SignUpRequestValidator : RequestValidator<SignUpRequest>
{
    protected override void ValidateRequest(SignUpRequest request)
    {
        var body = request.Body;

        var displayNameParts = new[] { body.FirstName, body.MiddleName, body.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();
        var displayNameLength = displayNameParts.Sum(value => value.Length)
            + Math.Max(0, displayNameParts.Length - 1);
        if (displayNameLength > 255)
        {
            throw ValidationError("Display name must not exceed 255 characters.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
