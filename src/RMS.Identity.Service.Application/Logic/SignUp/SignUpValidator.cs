using System.Net;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.SignUp;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed class SignUpValidator
{
    public void Validate(SignUpCommandRequest command)
    {
        if (!EmailAddressValidator.IsValid(command.EmailAddress))
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Email address must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8)
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Password must be at least 8 characters long.");
        }

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Phone number is required.");
        }

    }
}
