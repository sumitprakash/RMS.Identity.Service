using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record UpdateCompanyUserCommandRequest(
    Guid ActorUserUuid,
    Guid CompanyUuid,
    Guid UserUuid,
    string CompanyRole,
    string MembershipStatus) : ICommand<UpdateCompanyUserCommandResponse>;
