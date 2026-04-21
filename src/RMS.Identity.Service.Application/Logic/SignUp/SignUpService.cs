using System.Text.Json;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.SignUp;

namespace RMS.Identity.Service.Application.Logic.SignUp;

public sealed class SignUpService : ISignUpService
{
    private readonly ISignUpStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITextHasher _textHasher;
    private readonly SignUpValidator _validator = new();

    public SignUpService(ISignUpStore store, IPasswordHasher passwordHasher, ITextHasher textHasher)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _textHasher = textHasher;
    }

    public Task<SignUpUser> ExecuteAsync(SignUpCommand command, CancellationToken cancellationToken)
    {
        _validator.Validate(command);

        var normalizedUsername = EmailAddressValidator.Normalize(command.Username);
        var displayName = string.IsNullOrWhiteSpace(command.DisplayName) ? null : command.DisplayName.Trim();
        var requestHash = string.IsNullOrWhiteSpace(command.IdempotencyKey)
            ? null
            : _textHasher.Hash(JsonSerializer.Serialize(new
            {
                username = normalizedUsername,
                displayName,
                phone = command.Phone
            }));

        var verificationToken = Guid.NewGuid().ToString("N");
        var storageCommand = new SignUpStorageCommand(
            Guid.NewGuid(),
            normalizedUsername,
            _passwordHasher.Hash(command.Password),
            displayName,
            verificationToken,
            _textHasher.Hash(verificationToken),
            DateTime.UtcNow.AddDays(1),
            command.IdempotencyKey,
            requestHash);

        // OpenAPI includes `phone`, but the canonical SQL schema has no phone column on UserAccount.
        return _store.ExecuteAsync(storageCommand, cancellationToken);
    }
}
