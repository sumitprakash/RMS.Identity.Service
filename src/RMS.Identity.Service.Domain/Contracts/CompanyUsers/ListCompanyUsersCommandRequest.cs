using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record ListCompanyUsersCommandRequest(
    Guid CompanyUuid) : ICommand<ListCompanyUsersCommandResponse>;
