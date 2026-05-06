using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.Logic.SignUp;
using RMS.Identity.Service.Application.Shared.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.SignUp;

namespace RMS.Identity.Service.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<IIdempotencyRequestFactory, IdempotencyRequestFactory>();
        services.AddScoped<ISignUpCommand, SignUpCommand>();
        return services;
    }
}
