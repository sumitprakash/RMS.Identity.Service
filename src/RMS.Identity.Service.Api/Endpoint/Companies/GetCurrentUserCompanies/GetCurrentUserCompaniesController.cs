using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Companies.GetCurrentUserCompanies;

[ApiController]
[Route("api/v1/current-user/companies")]
public sealed class GetCurrentUserCompaniesController : ControllerBase
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly IDatabaseTransactionExecutor _databaseTransactionExecutor;
    private readonly ICommandHandler<GetCurrentUserCompaniesCommandRequest, GetCurrentUserCompaniesCommandResponse> _getCurrentUserCompaniesCommandHandler;

    public GetCurrentUserCompaniesController(
        IAccessTokenUserResolver accessTokenUserResolver,
        IDatabaseTransactionExecutor databaseTransactionExecutor,
        ICommandHandler<GetCurrentUserCompaniesCommandRequest, GetCurrentUserCompaniesCommandResponse> getCurrentUserCompaniesCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _databaseTransactionExecutor = databaseTransactionExecutor;
        _getCurrentUserCompaniesCommandHandler = getCurrentUserCompaniesCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CurrentUserCompaniesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var userUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);
        var companies = await _databaseTransactionExecutor.ExecuteAsync(
            transactionCancellationToken => _getCurrentUserCompaniesCommandHandler.HandleAsync(
                new GetCurrentUserCompaniesCommandRequest(userUuid),
                transactionCancellationToken),
            cancellationToken);

        return Ok(companies.ToResponse());
    }
}
