using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.Identity;

namespace RMS.Identity.Service.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<SignUpUserUseCase>();
        services.AddScoped<VerifyEmailUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<GetUserUseCase>();
        services.AddScoped<GetCompanyUseCase>();

        return services;
    }
}
