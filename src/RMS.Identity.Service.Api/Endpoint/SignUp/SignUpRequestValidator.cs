using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed class SignUpRequestValidator
{
    public void Validate(SignUpRequestBody request)
    {
        if (!EmailAddressValidator.IsValid(request.EmailAddress))
        {
            throw ValidationError("Email address must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw ValidationError("Password must be at least 8 characters long.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw ValidationError("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw ValidationError("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw ValidationError("Phone number is required.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", message);
}
