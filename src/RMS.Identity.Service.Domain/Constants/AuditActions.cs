namespace RMS.Identity.Service.Domain.Constants;

public static class AuditActions
{
    public const string Created = "created";
    public const string LoggedIn = "login";
    public const string RefreshedToken = "refresh";
    public const string EmailVerified = "verify_email";
}
