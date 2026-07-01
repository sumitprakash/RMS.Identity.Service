using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Domain.Contracts.Login;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth/login")]
public sealed class LoginController : ControllerBase
{
    private readonly ICommandHandler<LoginCommandRequest, LoginCommandResponse> _commandHandler;

    public LoginController(ICommandHandler<LoginCommandRequest, LoginCommandResponse> commandHandler)
    {
        _commandHandler = commandHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PostAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = await _commandHandler.HandleAsync(request.ToCommand(), cancellationToken);
        return Ok(login.ToResponse());
    }
}
