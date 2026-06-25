using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using RMS.Identity.Service.Api.Shared.Validation;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

public sealed class RegisterCompanyRequestValidator : RequestValidator<RegisterCompanyRequest>
{
    private static readonly Regex GstinValidator = new(
        "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));
    private static readonly PhoneAttribute PhoneValidator = new();

    public override void Validate(RegisterCompanyRequest request)
    {
        var body = request.Body;

        if (string.IsNullOrWhiteSpace(body.LegalName))
        {
            throw ValidationError("Company legal name is required.");
        }

        if (string.IsNullOrWhiteSpace(body.Gstin))
        {
            throw ValidationError("GSTIN is required.");
        }

        if (!GstinValidator.IsMatch(body.Gstin.Trim()))
        {
            throw ValidationError("GSTIN must be a valid GSTIN.");
        }

        if (!EmailAddressValidator.IsValid(body.ContactEmailAddress))
        {
            throw ValidationError("Company contact email address must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(body.ContactPhoneNumber))
        {
            throw ValidationError("Company contact phone number is required.");
        }

        if (!PhoneValidator.IsValid(body.ContactPhoneNumber))
        {
            throw ValidationError("Company contact phone number must be a valid phone number.");
        }

        if (string.IsNullOrWhiteSpace(body.AddressLine1))
        {
            throw ValidationError("Company registered address line 1 is required.");
        }

        if (string.IsNullOrWhiteSpace(body.City))
        {
            throw ValidationError("Company city is required.");
        }

        if (string.IsNullOrWhiteSpace(body.State))
        {
            throw ValidationError("Company state is required.");
        }

        if (string.IsNullOrWhiteSpace(body.PostalCode))
        {
            throw ValidationError("Company postal code is required.");
        }

        if (string.IsNullOrWhiteSpace(body.Country))
        {
            throw ValidationError("Company country is required.");
        }
    }

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
