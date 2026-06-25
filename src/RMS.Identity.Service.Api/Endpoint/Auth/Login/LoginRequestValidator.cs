using System.ComponentModel.DataAnnotations;
using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

public sealed class LoginRequestValidator : RequestValidator<LoginRequest>
{
    private static readonly EmailAddressAttribute EmailAddressValidator = new();

    public override void Validate(LoginRequest request)
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

        if (body.Username.Length > 150)
        {
            throw ValidationError("Username must not exceed 150 characters.");
        }

        if (string.IsNullOrWhiteSpace(body.Password))
        {
            throw ValidationError("Password is required.");
        }

        if (body.Password.Length > 128)
        {
            throw ValidationError("Password must not exceed 128 characters.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
