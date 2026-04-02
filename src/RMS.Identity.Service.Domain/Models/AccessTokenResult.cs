namespace RMS.Identity.Service.Domain.Models;

public sealed record AccessTokenResult(string Token, int ExpiresInSeconds);
