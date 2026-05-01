using Microsoft.AspNetCore.Mvc;
using RMS.Identity.Service.Domain.Interfaces.SignUp;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

[ApiController]
[Route("api/v1/signup")]
public sealed class SignUpController : ControllerBase
{
    private readonly ISignUpService _service;

    public SignUpController(ISignUpService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync(SignUpRequestBody body, CancellationToken cancellationToken)
    {
        var request = SignUpRequest.FromHttpRequest(Request, body);
        var user = await _service.ExecuteAsync(request.ToCommand(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }
}
