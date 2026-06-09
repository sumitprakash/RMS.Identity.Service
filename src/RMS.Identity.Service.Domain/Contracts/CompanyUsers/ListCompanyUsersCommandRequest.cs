using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.CompanyUsers;

public sealed record ListCompanyUsersCommandRequest(
    Guid CompanyUuid) : ICommand<ListCompanyUsersCommandResponse>;
