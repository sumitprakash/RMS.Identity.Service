using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Infrastructure.Notifications;

namespace RMS.Identity.Service.Infrastructure.Outbox;

public sealed class EmailVerificationEndpointClient : IEmailVerificationEndpointClient
{
    private readonly HttpClient _httpClient;
    private readonly EmailDeliveryOptions _options;

    public EmailVerificationEndpointClient(
        HttpClient httpClient,
        IOptions<EmailDeliveryOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task VerifyAsync(
        string token,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            _options.VerifyEmailEndpointUrl,
            new VerifyEmailRequest(token),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private sealed record VerifyEmailRequest(string Token);
}
