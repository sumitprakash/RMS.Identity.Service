using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class UpdateCompanyCommandHandler : ICommandHandler<UpdateCompanyCommandRequest, UpdateCompanyCommandResponse>
{
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
        var legalName = command.LegalName.Trim();
        var gstin = command.Gstin.Trim().ToUpperInvariant();
        var contactEmailAddress = EmailAddressValidator.Normalize(command.ContactEmailAddress);
        var contactPhoneNumber = command.ContactPhoneNumber.Trim();
        var tradeName = TrimToNull(command.TradeName);
        var addressLine1 = command.AddressLine1.Trim();
        var addressLine2 = TrimToNull(command.AddressLine2);
        var city = command.City.Trim();
        var state = command.State.Trim();
        var postalCode = command.PostalCode.Trim();
        var country = command.Country.Trim().ToUpperInvariant();

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

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
