using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.DependencyInjection;
using RMS.Identity.Service.Application.Logic.SignUp;
using RMS.Identity.Service.Application.Shared.Security;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.SignUp;
using RMS.Identity.Service.Infrastructure.Security;

namespace RMS.Identity.Service.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityServiceApplication();
        services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITextHasher, Sha256TextHasher>();
        services.AddScoped<ISignUpStore, SignUpMySqlStore>();

        return services;
    }
}
