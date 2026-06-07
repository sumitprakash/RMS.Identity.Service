using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record CreateCompanyUserCommandRequest(
    Guid CompanyUuid,
    string Username,
    string? DisplayName,
    string CompanyRole) : ICommand<CreateCompanyUserCommandResponse>;
