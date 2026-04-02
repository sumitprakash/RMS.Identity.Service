using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.Logic.SignUp;

namespace RMS.Identity.Service.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<ISignUpService, SignUpService>();
        return services;
    }
}
