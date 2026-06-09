namespace RMS.Identity.Service.Infrastructure.Outbox;

public interface IEmailVerificationEndpointClient
{
    Task VerifyAsync(
        string token,
        CancellationToken cancellationToken);
}
