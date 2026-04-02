using System.Net;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed class SignUpValidator
{
    public void Validate(SignUpCommand command)
    {
        if (!EmailAddressValidator.IsValid(command.Username))
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Username must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8)
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Password must be at least 8 characters long.");
        }

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey) && !Guid.TryParse(command.IdempotencyKey, out _))
        {
            throw new ServiceException((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", "Idempotency-Key must be a valid UUID.");
        }
    }
}
