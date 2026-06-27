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

    public UpdateCompanyUserCommandHandler(
        ICompanyUserReadRepository companyUserReadRepository,
        ICompanyUserWriteRepository companyUserWriteRepository,
        IAuditLogWriteRepository auditLogWriteRepository)
    {
        _companyUserReadRepository = companyUserReadRepository;
        _companyUserWriteRepository = companyUserWriteRepository;
        _auditLogWriteRepository = auditLogWriteRepository;
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
