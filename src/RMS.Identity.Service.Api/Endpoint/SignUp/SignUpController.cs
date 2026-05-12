using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

[ApiController]
[Route("api/v1/signup")]
public sealed class SignUpController : ControllerBase
{
    private readonly ICommandHandler<SignUpCommandRequest, SignUpCommandResponse> _commandHandler;

    public SignUpController(ICommandHandler<SignUpCommandRequest, SignUpCommandResponse> commandHandler)
    {
        _commandHandler = commandHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync(SignUpRequestBody body, CancellationToken cancellationToken)
    {
        var user = await _commandHandler.HandleAsync(body.ToCommand(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }
}
