using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Api.Shared.Auth;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Admin.Companies.UpdateCompanyStatus;

[ApiController]
[Route("api/v1/admin/companies/{companyUuid:guid}/status")]
public sealed class UpdateCompanyStatusController : ControllerBase
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;
    private readonly IDatabaseTransactionExecutor _databaseTransactionExecutor;
    private readonly IPlatformAdminAuthorizer _platformAdminAuthorizer;
    private readonly ICommandHandler<UpdateCompanyStatusCommandRequest, UpdateCompanyStatusCommandResponse> _updateCompanyStatusCommandHandler;

    public UpdateCompanyStatusController(
        IAccessTokenUserResolver accessTokenUserResolver,
        IDatabaseTransactionExecutor databaseTransactionExecutor,
        IPlatformAdminAuthorizer platformAdminAuthorizer,
        ICommandHandler<UpdateCompanyStatusCommandRequest, UpdateCompanyStatusCommandResponse> updateCompanyStatusCommandHandler)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
        _databaseTransactionExecutor = databaseTransactionExecutor;
        _platformAdminAuthorizer = platformAdminAuthorizer;
        _updateCompanyStatusCommandHandler = updateCompanyStatusCommandHandler;
    }

    [HttpPatch]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchAsync(
        Guid companyUuid,
        UpdateCompanyStatusRequestBody body,
        CancellationToken cancellationToken)
    {
        var actorUserUuid = _accessTokenUserResolver.ResolveRequiredUserUuid(HttpContext);

        var company = await _databaseTransactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                await _platformAdminAuthorizer.AuthorizeAsync(actorUserUuid, transactionCancellationToken);

                return await _updateCompanyStatusCommandHandler.HandleAsync(
                    new UpdateCompanyStatusCommandRequest(actorUserUuid, companyUuid, body.Status),
                    transactionCancellationToken);
            },
            cancellationToken);

        return Ok(company.ToResponse());
    }
}
