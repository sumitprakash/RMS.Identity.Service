using System.Net;
using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Entities.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Cqrs;

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
            throw new ServiceException(
                (int)HttpStatusCode.NotFound,
                "COMPANY_USER_NOT_FOUND",
                "Company user could not be found.");
        }

        return new GetCompanyUserCommandResponse(
            user.UserUuid,
            user.Username,
            user.DisplayName,
            Array.Empty<string>(),
            user.CompanyRole,
            ResolveStatus(user),
            user.CreatedAt);
    }

    private static string ResolveStatus(CompanyUserAccount user)
    {
        if (!user.IsActive || string.Equals(user.MembershipStatus, "suspended", StringComparison.OrdinalIgnoreCase))
        {
            return "suspended";
        }

        return user.EmailVerified ? "active" : "pending";
    }
}
