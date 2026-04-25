using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.DependencyInjection;
using RMS.Identity.Service.Domain.Interfaces.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Idempotency;
using RMS.Identity.Service.Infrastructure.Persistence.SignUp;
using RMS.Identity.Service.Infrastructure.Security;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.SignUp;

namespace RMS.Identity.Service.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityServiceApplication();
        services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();
        services.AddScoped<IDatabaseTransactionExecutor, MySqlDatabaseTransactionExecutor>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITextHasher, Sha256TextHasher>();
        services.AddScoped<IIdempotencyCoordinator, IdempotencyCoordinator>();
        services.AddScoped<IIdempotencyRepository, IdempotencyMySqlRepository>();
        services.AddScoped<IUserAccountRepository, UserAccountMySqlRepository>();
        services.AddScoped<IEmailVerificationRepository, EmailVerificationMySqlRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogMySqlRepository>();
        services.AddScoped<IVerificationEmailOutboxRepository, VerificationEmailOutboxMySqlRepository>();

        return services;
    }
}
