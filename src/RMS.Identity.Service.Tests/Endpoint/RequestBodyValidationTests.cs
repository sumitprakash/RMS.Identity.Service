using System.ComponentModel.DataAnnotations;
using RMS.Identity.Service.Api.Endpoint.Auth.Refresh;
using RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;
using RMS.Identity.Service.Api.Endpoint.Companies.UpdateCompany;
using RMS.Identity.Service.Api.Endpoint.SignUp;
using RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

namespace RMS.Identity.Service.Tests.Endpoint;

public sealed class RequestBodyValidationTests
{
    [Fact]
    public void SignUpRequestBody_WithBlankName_IsInvalid()
    {
        var body = new SignUpRequestBody
        {
            EmailAddress = "alice@example.com",
            Password = "StrongPass@123",
            FirstName = "   ",
            LastName = "Example",
            PhoneNumber = "9876543210"
        };

        Assert.Contains(Validate(body), error => error.MemberNames.Contains(nameof(SignUpRequestBody.FirstName)));
    }

    [Fact]
    public void RegisterCompanyRequestBody_WithBlankCompanyFields_IsInvalid()
    {
        var body = CreateRegisterCompanyBody();
        body = WithBlankValues(body);

        var errors = Validate(body);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterCompanyRequestBody.LegalName)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterCompanyRequestBody.AddressLine1)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterCompanyRequestBody.City)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterCompanyRequestBody.State)));
    }

    [Fact]
    public void UpdateCompanyRequestBody_WithBlankCompanyFields_IsInvalid()
    {
        var body = new UpdateCompanyRequestBody
        {
            LegalName = "   ",
            Gstin = "29ABCDE1234F1Z5",
            ContactEmailAddress = "accounts@example.com",
            ContactPhoneNumber = "9876543211",
            AddressLine1 = "   ",
            City = "   ",
            State = "   ",
            PostalCode = "560001"
        };

        var errors = Validate(body);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UpdateCompanyRequestBody.LegalName)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UpdateCompanyRequestBody.AddressLine1)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UpdateCompanyRequestBody.City)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UpdateCompanyRequestBody.State)));
    }

    [Fact]
    public void VerifyEmailRequestBody_WithBlankToken_IsInvalid()
    {
        var body = new VerifyEmailRequestBody
        {
            Token = "                                "
        };

        Assert.Contains(Validate(body), error => error.MemberNames.Contains(nameof(VerifyEmailRequestBody.Token)));
    }

    [Fact]
    public void RefreshRequestBody_WithBlankRefreshToken_IsInvalid()
    {
        var body = new RefreshRequestBody
        {
            RefreshToken = "                                                                "
        };

        Assert.Contains(Validate(body), error => error.MemberNames.Contains(nameof(RefreshRequestBody.RefreshToken)));
    }

    private static RegisterCompanyRequestBody CreateRegisterCompanyBody() =>
        new()
        {
            LegalName = "Example Retail Pvt Ltd",
            Gstin = "29ABCDE1234F1Z5",
            ContactEmailAddress = "accounts@example.com",
            ContactPhoneNumber = "9876543211",
            AddressLine1 = "1 Main Road",
            City = "Bengaluru",
            State = "Karnataka",
            PostalCode = "560001"
        };

    private static IReadOnlyCollection<ValidationResult> Validate(object instance)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            validationResults,
            validateAllProperties: true);

        return validationResults;
    }

    private static RegisterCompanyRequestBody WithBlankValues(RegisterCompanyRequestBody body) =>
        new()
        {
            LegalName = "   ",
            Gstin = body.Gstin,
            ContactEmailAddress = body.ContactEmailAddress,
            ContactPhoneNumber = body.ContactPhoneNumber,
            AddressLine1 = "   ",
            City = "   ",
            State = "   ",
            PostalCode = body.PostalCode
        };
}
