using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Api.Shared.Auth;

public sealed class BearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAccessTokenUserResolver _accessTokenUserResolver;

    public BearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAccessTokenUserResolver accessTokenUserResolver)
        : base(options, logger, encoder)
    {
        _accessTokenUserResolver = accessTokenUserResolver;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var user = _accessTokenUserResolver.ResolveRequiredUser(Context);
            var userUuid = user.UserUuid.ToString();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userUuid),
                new("sub", userUuid)
            };

            if (!string.IsNullOrWhiteSpace(user.Username))
            {
                claims.Add(new Claim(ClaimTypes.Name, user.Username));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (ServiceException exception)
        {
            return Task.FromResult(AuthenticateResult.Fail(exception.Message));
        }
    }
}
