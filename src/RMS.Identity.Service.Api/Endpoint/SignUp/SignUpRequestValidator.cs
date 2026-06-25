using System.ComponentModel.DataAnnotations;
using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed class SignUpRequestValidator : RequestValidator<SignUpRequest>
{
    private static readonly PhoneAttribute PhoneValidator = new();

    public override void Validate(SignUpRequest request)
    {
        var body = request.Body;

        if (!EmailAddressValidator.IsValid(body.EmailAddress))
        {
            throw ValidationError("Email address must be a valid email address.");
        }

        if (body.EmailAddress.Length > 150)
        {
            throw ValidationError("Email address must not exceed 150 characters.");
        }

        if (string.IsNullOrWhiteSpace(body.Password) || body.Password.Length < 8)
        {
            throw ValidationError("Password must be at least 8 characters long.");
        }

        if (body.Password.Length > 128)
        {
            throw ValidationError("Password must not exceed 128 characters.");
        }

        if (string.IsNullOrWhiteSpace(body.FirstName))
        {
            throw ValidationError("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(body.LastName))
        {
            throw ValidationError("Last name is required.");
        }

        if (body.FirstName.Length > 100
            || body.LastName.Length > 100
            || (body.MiddleName?.Length ?? 0) > 100)
        {
            throw ValidationError("Each name component must not exceed 100 characters.");
        }

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

        if (string.IsNullOrWhiteSpace(body.PhoneNumber))
        {
            throw ValidationError("Phone number is required.");
        }

        if (!PhoneValidator.IsValid(body.PhoneNumber))
        {
            throw ValidationError("Phone number must be a valid phone number.");
        }

        if (body.PhoneNumber.Length > 32)
        {
            throw ValidationError("Phone number must not exceed 32 characters.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
