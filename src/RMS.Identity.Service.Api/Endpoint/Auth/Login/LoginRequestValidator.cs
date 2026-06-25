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

        if (string.IsNullOrWhiteSpace(body.Password))
        {
            throw ValidationError("Password is required.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
