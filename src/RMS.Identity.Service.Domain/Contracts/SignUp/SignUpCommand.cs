namespace RMS.Identity.Service.Domain.Contracts.SignUp;

public sealed record SignUpCommand(
    string EmailAddress,
    string Password,
    string FirstName,
    string? MiddleName,
    string LastName,
    string PhoneNumber,
    Guid IdempotencyKey);
