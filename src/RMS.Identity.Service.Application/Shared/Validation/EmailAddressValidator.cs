using System.Net.Mail;

namespace RMS.Identity.Service.Application.Shared.Validation;

public static class EmailAddressValidator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
