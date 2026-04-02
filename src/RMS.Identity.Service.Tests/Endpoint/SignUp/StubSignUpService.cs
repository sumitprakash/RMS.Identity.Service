using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Domain.Entities.SignUp;
using RMS.Identity.Service.Domain.Interfaces.SignUp;

namespace RMS.Identity.Service.Tests.Endpoint.SignUp;

public sealed class StubSignUpService : ISignUpService
{
    public Func<SignUpCommand, CancellationToken, Task<SignUpUser>> Handler { get; set; } =
        (command, _) => Task.FromResult(new SignUpUser(
            Guid.NewGuid(),
            command.Username,
            command.DisplayName,
            "pending",
            DateTime.UtcNow));

    public SignUpCommand? LastCommand { get; private set; }

    public Task<SignUpUser> ExecuteAsync(SignUpCommand command, CancellationToken cancellationToken)
    {
        LastCommand = command;
        return Handler(command, cancellationToken);
    }
}
