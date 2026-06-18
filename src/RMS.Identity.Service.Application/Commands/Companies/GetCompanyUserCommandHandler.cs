using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class GetCompanyUserCommandHandler : ICommandHandler<GetCompanyUserCommandRequest, GetCompanyUserCommandResponse>
{
    private readonly ICompanyUserReadRepository _companyUserReadRepository;

    public GetCompanyUserCommandHandler(ICompanyUserReadRepository companyUserReadRepository)
    {
        _companyUserReadRepository = companyUserReadRepository;
    }

    public async Task<GetCompanyUserCommandResponse> HandleAsync(
        GetCompanyUserCommandRequest command,
        CancellationToken cancellationToken)
    {
        var user = await _companyUserReadRepository.GetByCompanyAndUserUuidAsync(
            command.CompanyUuid,
            command.UserUuid,
            cancellationToken);

        if (user is null)
        {
            throw new ResourceNotFoundException(ServiceErrorDefinitions.CompanyUsers.CompanyUserNotFound);
        }

        return new GetCompanyUserCommandResponse(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            Array.Empty<string>(),
            user.CompanyRole,
            CompanyUserStatusResolver.Resolve(user),
            user.CreatedAt);
    }
}
