namespace RMS.Identity.Service.Application.Commands.SignUp;

public sealed class EmailVerificationOptions
{
    public const string SectionName = "EmailVerification";

    public bool AutoVerifyOnSignUp { get; init; }
}
