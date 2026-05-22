using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.DependencyInjection;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Idempotency;
using RMS.Identity.Service.Infrastructure.Persistence.Auth;
using RMS.Identity.Service.Infrastructure.Persistence.AuditLog;
using RMS.Identity.Service.Infrastructure.Persistence.UserAccounts;
using RMS.Identity.Service.Infrastructure.Security;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Security;

namespace RMS.Identity.Service.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityServiceApplication();
        services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();
        services.AddScoped<IDatabaseTransactionAccessor, DatabaseTransactionAccessor>();
        services.AddScoped<IDatabaseTransactionExecutor, MySqlDatabaseTransactionExecutor>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthTokenGenerator, JwtAuthTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<ITextHasher, Sha256TextHasher>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.SigningKeyEnvVar))
                {
                    return;
                }

                options.SigningKey = configuration[options.SigningKeyEnvVar] ?? string.Empty;
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
            .Validate(options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "JWT signing key must be at least 32 bytes.")
            .Validate(options => options.AccessTokenLifetimeSeconds > 0, "JWT access token lifetime must be greater than zero.")
            .Validate(options => options.RefreshTokenLifetimeDays > 0, "JWT refresh token lifetime must be greater than zero.")
            .ValidateOnStart();
        services.AddScoped<IAuthenticationRepository, AuthenticationMySqlRepository>();
        services.AddScoped<IdempotencyMySqlRepository>();
        services.AddScoped<IIdempotencyReadRepository>(
            provider => provider.GetRequiredService<IdempotencyMySqlRepository>());
        services.AddScoped<IIdempotencyWriteRepository>(
            provider => provider.GetRequiredService<IdempotencyMySqlRepository>());
        services.AddScoped<UserAccountMySqlRepository>();
        services.AddScoped<IUserAccountReadRepository>(
            provider => provider.GetRequiredService<UserAccountMySqlRepository>());
        services.AddScoped<IUserAccountWriteRepository>(
            provider => provider.GetRequiredService<UserAccountMySqlRepository>());
        services.AddScoped<AuditLogMySqlRepository>();
        services.AddScoped<IAuditLogWriteRepository>(
            provider => provider.GetRequiredService<AuditLogMySqlRepository>());

        return services;
    }
}
