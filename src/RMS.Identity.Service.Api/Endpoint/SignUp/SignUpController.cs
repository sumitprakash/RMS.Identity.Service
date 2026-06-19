using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

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
    [ServiceFilter(typeof(SignUpRequestValidationFilter))]
    [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        var user = await _commandHandler.HandleAsync(request.ToCommand(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }
}
