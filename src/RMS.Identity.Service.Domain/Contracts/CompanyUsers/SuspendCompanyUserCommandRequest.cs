using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record SuspendCompanyUserCommandRequest(
    Guid ActorUserUuid,
    Guid CompanyUuid,
    Guid UserUuid) : ICommand<SuspendCompanyUserCommandResponse>;
