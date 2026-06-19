using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Domain.Contracts.Refresh;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Refresh;

[ApiController]
[Route("api/v1/auth/refresh")]
public sealed class RefreshController : ControllerBase
{
    private readonly ICommandHandler<RefreshCommandRequest, RefreshCommandResponse> _commandHandler;

    public RefreshController(ICommandHandler<RefreshCommandRequest, RefreshCommandResponse> commandHandler)
    {
        _commandHandler = commandHandler;
    }

    [HttpPost]
    [ServiceFilter(typeof(RefreshRequestValidationFilter))]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PostAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var refresh = await _commandHandler.HandleAsync(request.ToCommand(), cancellationToken);
        return Ok(refresh.ToResponse());
    }
}
