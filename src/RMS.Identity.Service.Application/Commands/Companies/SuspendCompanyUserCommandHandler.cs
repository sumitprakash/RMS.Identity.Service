using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Application.Commands.Companies;

public sealed class SuspendCompanyUserCommandHandler : ICommandHandler<SuspendCompanyUserCommandRequest, SuspendCompanyUserCommandResponse>
{
    private readonly ICommandHandler<UpdateCompanyUserCommandRequest, UpdateCompanyUserCommandResponse> _updateCompanyUserCommandHandler;
    private readonly ICompanyUserReadRepository _companyUserReadRepository;

    public SuspendCompanyUserCommandHandler(
        ICommandHandler<UpdateCompanyUserCommandRequest, UpdateCompanyUserCommandResponse> updateCompanyUserCommandHandler,
        ICompanyUserReadRepository companyUserReadRepository)
    {
        _updateCompanyUserCommandHandler = updateCompanyUserCommandHandler;
        _companyUserReadRepository = companyUserReadRepository;
    }

    public async Task<SuspendCompanyUserCommandResponse> HandleAsync(
        SuspendCompanyUserCommandRequest command,
        CancellationToken cancellationToken)
    {
        var user = await _companyUserReadRepository.GetByCompanyAndUserUuidAsync(
            command.CompanyUuid,
            command.UserUuid,
            cancellationToken);

        await _updateCompanyUserCommandHandler.HandleAsync(
            new UpdateCompanyUserCommandRequest(
                command.ActorUserUuid,
                command.CompanyUuid,
                command.UserUuid,
                user?.CompanyRole ?? "MEMBER",
                "suspended"),
            cancellationToken);

        return new SuspendCompanyUserCommandResponse();
    }
}
