using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.ListCompanyUsers;

[ApiController]
[Route("api/v1/companies/{companyUuid:guid}/users")]
public sealed class ListCompanyUsersController : ControllerBase
{
    private static readonly string[] CompanyUserManagerRoles = ["OWNER", "ADMIN"];

    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly ICompanyAccessAuthorizer _companyAccessAuthorizer;
    private readonly IDatabaseTransactionExecutor _databaseTransactionExecutor;
    private readonly ICommandHandler<ListCompanyUsersCommandRequest, ListCompanyUsersCommandResponse> _listCompanyUsersCommandHandler;

    public ListCompanyUsersController(
        IAccessTokenUserResolver accessTokenUserResolver,
        ICompanyAccessAuthorizer companyAccessAuthorizer,
        IDatabaseTransactionExecutor databaseTransactionExecutor,
        ICommandHandler<ListCompanyUsersCommandRequest, ListCompanyUsersCommandResponse> listCompanyUsersCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _companyAccessAuthorizer = companyAccessAuthorizer;
        _databaseTransactionExecutor = databaseTransactionExecutor;
        _listCompanyUsersCommandHandler = listCompanyUsersCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListCompanyUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAsync(Guid companyUuid, CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);

        var users = await _databaseTransactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                await _companyAccessAuthorizer.AuthorizeRoleAsync(
                    actorUserUuid,
                    companyUuid,
                    CompanyUserManagerRoles,
                    transactionCancellationToken);

                return await _listCompanyUsersCommandHandler.HandleAsync(
                    new ListCompanyUsersCommandRequest(companyUuid),
                    transactionCancellationToken);
            },
            cancellationToken);

        return Ok(users.ToResponse());
    }
}
