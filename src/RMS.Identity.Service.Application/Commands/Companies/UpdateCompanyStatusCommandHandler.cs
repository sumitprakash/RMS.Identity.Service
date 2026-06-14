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

    public UpdateCompanyStatusCommandHandler(
        IAuditLogWriteRepository auditLogWriteRepository,
        ICompanyReadRepository companyReadRepository,
        ICompanyWriteRepository companyWriteRepository,
        IUserAccountReadRepository userAccountReadRepository)
    {
        _auditLogWriteRepository = auditLogWriteRepository;
        _companyReadRepository = companyReadRepository;
        _companyWriteRepository = companyWriteRepository;
        _userAccountReadRepository = userAccountReadRepository;
    }

    public async Task<UpdateCompanyStatusCommandResponse> HandleAsync(
        UpdateCompanyStatusCommandRequest command,
        CancellationToken cancellationToken)
    {
        var targetStatus = NormalizeStatus(command.Status);
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

        return ToResponse(updatedCompany);
    }

    private static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw ValidationError("Company status is required.");
        }

        var normalized = status.Trim().ToLowerInvariant();
        if (!AllowedTransitions.ContainsKey(normalized)
            && !AllowedTransitions.Values.Any(targets => targets.Contains(normalized, StringComparer.Ordinal)))
        {
            throw ValidationError("Company status must be verified, rejected, or suspended.");
        }

        return normalized;
    }

    private static void EnsureValidTransition(string currentStatus, string targetStatus)
    {
        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowedTargets)
            || !allowedTargets.Contains(targetStatus, StringComparer.Ordinal))
        {
            throw new ConflictException($"Company status cannot transition from {currentStatus} to {targetStatus}.");
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

    private static ServiceException ValidationError(string message) =>
        new BadRequestException(message);
}
