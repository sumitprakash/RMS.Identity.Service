namespace RMS.Identity.Service.Application.DTOs
{
    public sealed class AuthResult
    {
        public string AccessToken { get; init; } = null!;
        public string RefreshToken { get; init; } = null!;
    }
}