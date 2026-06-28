using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class UpdateCompanyCommandHandler : ICommandHandler<UpdateCompanyCommandRequest, UpdateCompanyCommandResponse>
{
    private static readonly Regex GstinValidator = new(
        "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));
    private readonly ICompanyReadRepository _companyReadRepository;
    private readonly ICompanyWriteRepository _companyWriteRepository;
    private readonly ILogger<UpdateCompanyCommandHandler> _logger;

    public UpdateCompanyCommandHandler(
        ICompanyReadRepository companyReadRepository,
        ICompanyWriteRepository companyWriteRepository,
        ILogger<UpdateCompanyCommandHandler> logger)
    {
        _companyReadRepository = companyReadRepository;
        _companyWriteRepository = companyWriteRepository;
        _logger = logger;
    }

    public async Task<UpdateCompanyCommandResponse> HandleAsync(
        UpdateCompanyCommandRequest command,
        CancellationToken cancellationToken)
    {
        var updateCommand = Normalize(command);
        await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        await _companyWriteRepository.UpdateAsync(updateCommand, cancellationToken);

        var company = await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        _logger.LogInformation("Updated company {CompanyUuid}.", company.CompanyUuid);
        return new UpdateCompanyCommandResponse(
            company.CompanyUuid,
            CompanyCode: null,
            company.LegalName,
            company.TradeName,
            company.Gstin,
            company.ContactEmailAddress,
            company.ContactPhoneNumber,
            company.AddressLine1,
            company.AddressLine2,
            company.City,
            company.State,
            company.PostalCode,
            company.Country,
            company.Status);
    }

    private static UpdateCompanyCommand Normalize(UpdateCompanyCommandRequest command)
    {
        var legalName = TrimRequired(command.LegalName, "Company legal name is required.");
        EnsureMaxLength(legalName, 255, "Company legal name");
        var gstin = TrimRequired(command.Gstin, "GSTIN is required.").ToUpperInvariant();
        if (!GstinValidator.IsMatch(gstin))
        {
            throw ValidationError("GSTIN must be a valid GSTIN.");
        }

        var contactEmailAddress = EmailAddressValidator.Normalize(command.ContactEmailAddress);
        EnsureMaxLength(contactEmailAddress, 150, "Company contact email address");
        var contactPhoneNumber = TrimRequired(command.ContactPhoneNumber, "Company contact phone number is required.");
        if (!IsTenDigitPhoneNumber(contactPhoneNumber))
        {
            throw ValidationError("Company contact phone number must be exactly 10 digits.");
        }

        var tradeName = TrimToNull(command.TradeName);
        var addressLine1 = TrimRequired(command.AddressLine1, "Company registered address line 1 is required.");
        var addressLine2 = TrimToNull(command.AddressLine2);
        var city = TrimRequired(command.City, "Company city is required.");
        var state = TrimRequired(command.State, "Company state is required.");
        var postalCode = TrimRequired(command.PostalCode, "Company postal code is required.");
        var country = TrimRequired(command.Country, "Company country is required.").ToUpperInvariant();
        EnsureMaxLength(tradeName, 255, "Company trade name");
        EnsureMaxLength(addressLine1, 255, "Company registered address line 1");
        EnsureMaxLength(addressLine2, 255, "Company registered address line 2");
        EnsureMaxLength(city, 128, "Company city");
        EnsureMaxLength(state, 128, "Company state");
        if (!IsSixDigitPostalCode(postalCode))
        {
            throw ValidationError("Company postal code must be exactly 6 digits.");
        }

        if (country.Length != 2)
        {
            throw ValidationError("Company country must be a two-letter country code.");
        }

        return new UpdateCompanyCommand(
            command.CompanyUuid,
            legalName,
            tradeName,
            gstin,
            contactEmailAddress,
            contactPhoneNumber,
            addressLine1,
            addressLine2,
            city,
            state,
            postalCode,
            country);
    }

    private static string TrimRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw ValidationError(message);
        }

        return value.Trim();
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureMaxLength(string? value, int maxLength, string fieldName)
    {
        if ((value?.Length ?? 0) > maxLength)
        {
            throw ValidationError($"{fieldName} must not exceed {maxLength} characters.");
        }
    }

    private static bool IsTenDigitPhoneNumber(string value) =>
        value.Length == 10 && value.All(char.IsDigit);

    private static bool IsSixDigitPostalCode(string value) =>
        value.Length == 6 && value.All(char.IsDigit);

    private static ServiceException ValidationError(string message) =>
        new ApplicationServiceException(ServiceStatusErrorCodes.BadRequest, message);
}
