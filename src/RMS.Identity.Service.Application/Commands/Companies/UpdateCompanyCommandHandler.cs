using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
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
    private static readonly PhoneAttribute PhoneValidator = new();

    private readonly ICompanyReadRepository _companyReadRepository;
    private readonly ICompanyWriteRepository _companyWriteRepository;

    public UpdateCompanyCommandHandler(
        ICompanyReadRepository companyReadRepository,
        ICompanyWriteRepository companyWriteRepository)
    {
        _companyReadRepository = companyReadRepository;
        _companyWriteRepository = companyWriteRepository;
    }

    public async Task<UpdateCompanyCommandResponse> HandleAsync(
        UpdateCompanyCommandRequest command,
        CancellationToken cancellationToken)
    {
        var updateCommand = Normalize(command);
        await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        await _companyWriteRepository.UpdateAsync(updateCommand, cancellationToken);

        var company = await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
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
        var gstin = TrimRequired(command.Gstin, "GSTIN is required.").ToUpperInvariant();
        if (!GstinValidator.IsMatch(gstin))
        {
            throw ValidationError("GSTIN must be a valid GSTIN.");
        }

        var contactEmailAddress = EmailAddressValidator.Normalize(command.ContactEmailAddress);
        var contactPhoneNumber = TrimRequired(command.ContactPhoneNumber, "Company contact phone number is required.");
        if (!PhoneValidator.IsValid(contactPhoneNumber))
        {
            throw ValidationError("Company contact phone number must be a valid phone number.");
        }

        return new UpdateCompanyCommand(
            command.CompanyUuid,
            legalName,
            TrimToNull(command.TradeName),
            gstin,
            contactEmailAddress,
            contactPhoneNumber,
            TrimRequired(command.AddressLine1, "Company registered address line 1 is required."),
            TrimToNull(command.AddressLine2),
            TrimRequired(command.City, "Company city is required."),
            TrimRequired(command.State, "Company state is required."),
            TrimRequired(command.PostalCode, "Company postal code is required."),
            TrimRequired(command.Country, "Company country is required.").ToUpperInvariant());
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

    private static ServiceException ValidationError(string message) =>
        new BadRequestException(message);
}
