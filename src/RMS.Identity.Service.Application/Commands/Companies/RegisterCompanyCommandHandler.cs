using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Application.Shared.Validation;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class RegisterCompanyCommandHandler : ICommandHandler<RegisterCompanyCommandRequest, RegisterCompanyCommandResponse>
{
    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly ICompanyReadRepository _companyReadRepository;
    private readonly ICompanyWriteRepository _companyWriteRepository;
    private readonly ICompanyUserWriteRepository _companyUserWriteRepository;

    public RegisterCompanyCommandHandler(
        IUserAccountReadRepository userAccountReadRepository,
        ICompanyReadRepository companyReadRepository,
        ICompanyWriteRepository companyWriteRepository,
        ICompanyUserWriteRepository companyUserWriteRepository)
    {
        _userAccountReadRepository = userAccountReadRepository;
        _companyReadRepository = companyReadRepository;
        _companyWriteRepository = companyWriteRepository;
        _companyUserWriteRepository = companyUserWriteRepository;
    }

    public async Task<RegisterCompanyCommandResponse> HandleAsync(
        RegisterCompanyCommandRequest command,
        CancellationToken cancellationToken)
    {
        var owner = await _userAccountReadRepository.GetByUuidAsync(command.OwnerUserUuid, cancellationToken);
        if (!owner.IsActive || owner.IsDeleted)
        {
            throw new ServiceException(403, "USER_NOT_ACTIVE", "User is not allowed to register a company.");
        }

        var normalizedGstin = NormalizeGstin(command.Gstin);
        if (await _companyReadRepository.ExistsByGstinAsync(normalizedGstin, cancellationToken))
        {
            throw new ServiceException(409, "COMPANY_EXISTS", "Company GSTIN already exists.");
        }

        var createCompanyCommand = new CreateCompanyCommand(
            Guid.NewGuid(),
            command.LegalName.Trim(),
            TrimToNull(command.TradeName),
            normalizedGstin,
            EmailAddressValidator.Normalize(command.ContactEmailAddress),
            command.ContactPhoneNumber.Trim(),
            command.AddressLine1.Trim(),
            TrimToNull(command.AddressLine2),
            command.City.Trim(),
            command.State.Trim(),
            command.PostalCode.Trim(),
            NormalizeCountry(command.Country),
            "pending_verification");
        var companyId = await _companyWriteRepository.CreateAsync(createCompanyCommand, cancellationToken);

        await _companyUserWriteRepository.CreateAsync(
            new CreateCompanyUserCommand(companyId, owner.UserId, "OWNER", "active"),
            cancellationToken);

        var company = await _companyReadRepository.GetByIdAsync(companyId, cancellationToken);
        return new RegisterCompanyCommandResponse(
            company.CompanyUuid,
            company.LegalName,
            company.TradeName,
            company.Gstin,
            company.Status,
            company.CreatedAt);
    }

    private static string NormalizeGstin(string gstin) =>
        gstin.Trim().ToUpperInvariant();

    private static string NormalizeCountry(string country) =>
        country.Trim().ToUpperInvariant();

    private static string? TrimToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
