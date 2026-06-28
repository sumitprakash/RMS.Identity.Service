using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record CreateCompanyUserCommandRequest(
    Guid CompanyUuid,
    string Username,
    string? DisplayName,
    CompanyRole CompanyRole,
    Guid ActorUserUuid = default) : ICommand<CreateCompanyUserCommandResponse>;
