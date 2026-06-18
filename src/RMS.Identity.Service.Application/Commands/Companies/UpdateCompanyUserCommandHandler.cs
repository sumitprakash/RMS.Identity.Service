using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class UpdateCompanyUserCommandHandler : ICommandHandler<UpdateCompanyUserCommandRequest, UpdateCompanyUserCommandResponse>
{
    private static readonly string[] AllowedCompanyRoles = ["OWNER", "ADMIN", "MEMBER"];
    private static readonly string[] AllowedMembershipStatuses = ["active", "invited", "suspended"];

    private readonly ICompanyUserReadRepository _companyUserReadRepository;
    private readonly ICompanyUserWriteRepository _companyUserWriteRepository;

    public UpdateCompanyUserCommandHandler(
        ICompanyUserReadRepository companyUserReadRepository,
        ICompanyUserWriteRepository companyUserWriteRepository)
    {
        _companyUserReadRepository = companyUserReadRepository;
        _companyUserWriteRepository = companyUserWriteRepository;
    }

    public async Task<UpdateCompanyUserCommandResponse> HandleAsync(
        UpdateCompanyUserCommandRequest command,
        CancellationToken cancellationToken)
    {
        var companyRole = NormalizeCompanyRole(command.CompanyRole);
        var membershipStatus = NormalizeMembershipStatus(command.MembershipStatus);
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
        return ToResponse(updatedUser);
    }

    private async Task<CompanyUserAccount> LoadUserAsync(
        Guid companyUuid,
        Guid userUuid,
        CancellationToken cancellationToken) =>
        await _companyUserReadRepository.GetByCompanyAndUserUuidAsync(companyUuid, userUuid, cancellationToken)
        ?? throw new ResourceNotFoundException(ServiceErrorDefinitions.CompanyUsers.CompanyUserNotFound);

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
            throw new ConflictException(ServiceErrorDefinitions.CompanyUsers.LastOwnerRequired);
        }
    }

    private static bool IsActiveOwner(CompanyUserAccount user) =>
        string.Equals(user.CompanyRole, "OWNER", StringComparison.OrdinalIgnoreCase)
        && string.Equals(user.MembershipStatus, "active", StringComparison.OrdinalIgnoreCase)
        && user.IsActive;

    private static string NormalizeCompanyRole(string companyRole)
    {
        var normalized = companyRole.Trim().ToUpperInvariant();
        if (!AllowedCompanyRoles.Contains(normalized, StringComparer.Ordinal))
        {
            throw ValidationError("Company role must be OWNER, ADMIN, or MEMBER.");
        }

        return normalized;
    }

    private static string NormalizeMembershipStatus(string membershipStatus)
    {
        var normalized = membershipStatus.Trim().ToLowerInvariant();
        if (!AllowedMembershipStatuses.Contains(normalized, StringComparer.Ordinal))
        {
            throw ValidationError("Membership status must be active, invited, or suspended.");
        }

        return normalized;
    }

    private static UpdateCompanyUserCommandResponse ToResponse(CompanyUserAccount user) =>
        new(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            Array.Empty<string>(),
            user.CompanyRole,
            CompanyUserStatusResolver.Resolve(user),
            user.CreatedAt);

    private static ServiceException ValidationError(string message) =>
        new BadRequestException(message);
}
