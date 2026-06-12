using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.SignUp;

public sealed record SignUpCommandRequest(
    string EmailAddress,
    string Password,
    string FirstName,
    string? MiddleName,
    string LastName,
    string PhoneNumber) : ICommand<SignUpCommandResponse>;
