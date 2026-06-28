using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class UpdateCompanyStatusCommandHandler : ICommandHandler<UpdateCompanyStatusCommandRequest, UpdateCompanyStatusCommandResponse>
{
    private static readonly Dictionary<string, string[]> AllowedTransitions = new(StringComparer.Ordinal)
    {
        ["pending_verification"] = ["verified", "rejected"],
        ["verified"] = ["suspended"],
        ["suspended"] = ["verified"]
    };

    private readonly IAuditLogWriteRepository _auditLogWriteRepository;
    private readonly ICompanyReadRepository _companyReadRepository;
    private readonly ICompanyWriteRepository _companyWriteRepository;
    private readonly IUserAccountReadRepository _userAccountReadRepository;
    private readonly ILogger<UpdateCompanyStatusCommandHandler> _logger;

    public UpdateCompanyStatusCommandHandler(
        IAuditLogWriteRepository auditLogWriteRepository,
        ICompanyReadRepository companyReadRepository,
        ICompanyWriteRepository companyWriteRepository,
        IUserAccountReadRepository userAccountReadRepository,
        ILogger<UpdateCompanyStatusCommandHandler> logger)
    {
        _auditLogWriteRepository = auditLogWriteRepository;
        _companyReadRepository = companyReadRepository;
        _companyWriteRepository = companyWriteRepository;
        _userAccountReadRepository = userAccountReadRepository;
        _logger = logger;
    }

    public async Task<UpdateCompanyStatusCommandResponse> HandleAsync(
        UpdateCompanyStatusCommandRequest command,
        CancellationToken cancellationToken)
    {
        var targetStatus = command.Status.ToStorageValue();
        var company = await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        EnsureValidTransition(company.Status, targetStatus);

        await _companyWriteRepository.UpdateStatusAsync(
            new UpdateCompanyStatusCommand(command.CompanyUuid, targetStatus),
            cancellationToken);

        var updatedCompany = await _companyReadRepository.GetByUuidAsync(command.CompanyUuid, cancellationToken);
        var actor = await _userAccountReadRepository.GetByUuidAsync(command.ActorUserUuid, cancellationToken);
        await _auditLogWriteRepository.InsertCompanyStatusChangedAsync(
            updatedCompany,
            company.Status,
            actor.UserId,
            cancellationToken);
        _logger.LogInformation(
            "Updated company {CompanyUuid} status from {PreviousStatus} to {CurrentStatus}.",
            updatedCompany.CompanyUuid,
            company.Status,
            updatedCompany.Status);

        return ToResponse(updatedCompany);
    }

    private static void EnsureValidTransition(string currentStatus, string targetStatus)
    {
        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedTargets)
            || !allowedTargets.Contains(targetStatus, StringComparer.Ordinal))
        {
            throw new ApplicationServiceException(ServiceErrorDefinitions.Companies.InvalidCompanyStatusTransition);
        }
    }

    private static UpdateCompanyStatusCommandResponse ToResponse(Company company) =>
        new(
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
