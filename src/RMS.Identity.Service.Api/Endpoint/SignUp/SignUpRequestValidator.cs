using System.ComponentModel.DataAnnotations;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed class SignUpRequestValidator
{
    private static readonly PhoneAttribute PhoneValidator = new();

    public void Validate(SignUpRequest request)
    {
        var body = request.Body;

        if (!EmailAddressValidator.IsValid(body.EmailAddress))
        {
            throw ValidationError("Email address must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(body.Password) || body.Password.Length < 8)
        {
            throw ValidationError("Password must be at least 8 characters long.");
        }

        if (string.IsNullOrWhiteSpace(body.FirstName))
        {
            throw ValidationError("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(body.LastName))
        {
            throw ValidationError("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(body.PhoneNumber))
        {
            throw ValidationError("Phone number is required.");
        }

        if (!PhoneValidator.IsValid(body.PhoneNumber))
        {
            throw ValidationError("Phone number must be a valid phone number.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new BadRequestException(message);
}
