using System.Net.Mail;
using RMS.Identity.Service.Domain.Exceptions;

namespace RMS.Identity.Service.Application.Identity.Internal;

internal static class InputValidation
{
    public static string RequireEmail(string email, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("invalid_input", $"{fieldName} is required");
        }

        var normalized = email.Trim().ToLowerInvariant();

        try
        {
            _ = new MailAddress(normalized);
            return normalized;
        }
        catch (FormatException)
        {
            throw new ValidationException("invalid_input", $"{fieldName} must be a valid email address");
        }
    }

    public static void RequirePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ValidationException("invalid_input", "password must be at least 8 characters long");
        }
    }

    public static string RequireToken(string token, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ValidationException("invalid_input", $"{fieldName} is required");
        }

        return token.Trim();
    }
}
