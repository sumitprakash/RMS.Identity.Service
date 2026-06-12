using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record UpdateCompanyUserCommandRequest(
    Guid ActorUserUuid,
    Guid CompanyUuid,
    Guid UserUuid,
    string CompanyRole,
    string MembershipStatus) : ICommand<UpdateCompanyUserCommandResponse>;
