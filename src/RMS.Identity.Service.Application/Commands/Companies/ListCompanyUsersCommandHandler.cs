using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class ListCompanyUsersCommandHandler : ICommandHandler<ListCompanyUsersCommandRequest, ListCompanyUsersCommandResponse>
{
    private readonly ICompanyUserReadRepository _companyUserReadRepository;

    public ListCompanyUsersCommandHandler(ICompanyUserReadRepository companyUserReadRepository)
    {
        _companyUserReadRepository = companyUserReadRepository;
    }

    public async Task<ListCompanyUsersCommandResponse> HandleAsync(
        ListCompanyUsersCommandRequest command,
        CancellationToken cancellationToken)
    {
        var users = await _companyUserReadRepository.ListByCompanyUuidAsync(command.CompanyUuid, cancellationToken);
        return new ListCompanyUsersCommandResponse(users.Select(ToResponse).ToArray());
    }

    private static CompanyUserResponseItem ToResponse(CompanyUserAccount user) =>
        new(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            Array.Empty<string>(),
            user.CompanyRole,
            CompanyUserStatusResolver.Resolve(user),
            user.CreatedAt);
}
