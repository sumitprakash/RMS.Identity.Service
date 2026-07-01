using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Domain.Contracts.VerifyEmail;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

[ApiController]
[AllowAnonymous]
[Route("api/v1/users/verify-email")]
public sealed class VerifyEmailController : ControllerBase
{
    private readonly IDatabaseTransactionExecutor _databaseTransactionExecutor;
    private readonly ICommandHandler<VerifyEmailCommandRequest, VerifyEmailCommandResponse> _verifyEmailCommandHandler;

    public VerifyEmailController(
        IDatabaseTransactionExecutor databaseTransactionExecutor,
        ICommandHandler<VerifyEmailCommandRequest, VerifyEmailCommandResponse> verifyEmailCommandHandler)
    {
        _databaseTransactionExecutor = databaseTransactionExecutor;
        _verifyEmailCommandHandler = verifyEmailCommandHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostAsync(
        VerifyEmailRequestBody body,
        CancellationToken cancellationToken)
    {
        var response = await _databaseTransactionExecutor.ExecuteAsync(
            transactionCancellationToken => _verifyEmailCommandHandler.HandleAsync(
                body.ToCommand(),
                transactionCancellationToken),
            cancellationToken);

        return Ok(response.ToResponse());
    }
}
