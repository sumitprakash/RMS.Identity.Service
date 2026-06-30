using Microsoft.Extensions.Logging;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class UpdateCompanyUserCommandHandler : ICommandHandler<UpdateCompanyUserCommandRequest, UpdateCompanyUserCommandResponse>
{
    private readonly ICompanyUserReadRepository _companyUserReadRepository;
    private readonly ICompanyUserWriteRepository _companyUserWriteRepository;
    private readonly IAuditLogWriteRepository _auditLogWriteRepository;
    private readonly ILogger<UpdateCompanyUserCommandHandler> _logger;

    public UpdateCompanyUserCommandHandler(
        ICompanyUserReadRepository companyUserReadRepository,
        ICompanyUserWriteRepository companyUserWriteRepository,
        IAuditLogWriteRepository auditLogWriteRepository,
        ILogger<UpdateCompanyUserCommandHandler> logger)
    {
        _companyUserReadRepository = companyUserReadRepository;
        _companyUserWriteRepository = companyUserWriteRepository;
        _auditLogWriteRepository = auditLogWriteRepository;
        _logger = logger;
    }

    public async Task<UpdateCompanyUserCommandResponse> HandleAsync(
        UpdateCompanyUserCommandRequest command,
        CancellationToken cancellationToken)
    {
        var companyRole = command.CompanyRole.ToStorageValue();
        var membershipStatus = command.MembershipStatus.ToStorageValue();
        var existingUser = await LoadUserAsync(command.CompanyUuid, command.UserUuid, cancellationToken);

        await PreventRemovingLastActiveOwnerAsync(
            command.CompanyUuid,
            existingUser,
            companyRole,
            membershipStatus,
            cancellationToken);

        await _companyUserWriteRepository.UpdateMembershipAsync(
            new UpdateCompanyUserCommand(
                command.CompanyUuid,
                command.UserUuid,
                companyRole,
                membershipStatus),
            cancellationToken);

        var updatedUser = await LoadUserAsync(command.CompanyUuid, command.UserUuid, cancellationToken);
        var action = string.Equals(membershipStatus, "suspended", StringComparison.Ordinal)
            ? "company_user_suspended"
            : "company_user_membership_updated";
        await _auditLogWriteRepository.InsertCompanyUserChangedAsync(
            action,
            command.ActorUserUuid,
            command.CompanyUuid,
            command.UserUuid,
            existingUser.CompanyRole,
            existingUser.MembershipStatus,
            updatedUser.CompanyRole,
            updatedUser.MembershipStatus,
            cancellationToken);
        _logger.LogInformation(
            "Updated company user {UserUuid} in company {CompanyUuid} to role {CompanyRole} and status {MembershipStatus}.",
            command.UserUuid,
            command.CompanyUuid,
            updatedUser.CompanyRole,
            updatedUser.MembershipStatus);

        return ToResponse(updatedUser);
    }

    private async Task<CompanyUserAccount> LoadUserAsync(
        Guid companyUuid,
        Guid userUuid,
        CancellationToken cancellationToken) =>
        await _companyUserReadRepository.GetByCompanyAndUserUuidAsync(companyUuid, userUuid, cancellationToken)
        ?? throw new ApplicationServiceException(ServiceErrorDefinitions.CompanyUsers.CompanyUserNotFound);

    private async Task PreventRemovingLastActiveOwnerAsync(
        Guid companyUuid,
        CompanyUserAccount existingUser,
        string newCompanyRole,
        string newMembershipStatus,
        CancellationToken cancellationToken)
    {
        if (!IsActiveOwner(existingUser))
        {
            return;
        }

        if (string.Equals(newCompanyRole, "OWNER", StringComparison.Ordinal)
            && string.Equals(newMembershipStatus, "active", StringComparison.Ordinal))
        {
            return;
        }

        if (await _companyUserWriteRepository.CountActiveOwnersForUpdateAsync(companyUuid, cancellationToken) <= 1)
        {
            _logger.LogWarning(
                "Company user update rejected because company {CompanyUuid} must keep at least one active owner.",
                companyUuid);
            throw new ApplicationServiceException(ServiceErrorDefinitions.CompanyUsers.LastOwnerRequired);
        }
    }

    private static bool IsActiveOwner(CompanyUserAccount user) =>
        string.Equals(user.CompanyRole, "OWNER", StringComparison.OrdinalIgnoreCase)
        && string.Equals(user.MembershipStatus, "active", StringComparison.OrdinalIgnoreCase)
        && user.IsActive;

    private static UpdateCompanyUserCommandResponse ToResponse(CompanyUserAccount user) =>
        new(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            Array.Empty<string>(),
            user.CompanyRole,
            CompanyUserStatusResolver.Resolve(user),
            user.CreatedAt);

}
