using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record GetCompanyUserCommandRequest(
    Guid CompanyUuid,
    Guid UserUuid) : ICommand<GetCompanyUserCommandResponse>;
