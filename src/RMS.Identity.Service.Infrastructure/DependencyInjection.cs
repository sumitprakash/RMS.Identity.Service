using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Security;
using RMS.Identity.Service.Domain.Interfaces.System;
using RMS.Identity.Service.Infrastructure.Persistence;
using RMS.Identity.Service.Infrastructure.Persistence.Internal;
using RMS.Identity.Service.Infrastructure.Security;
using RMS.Identity.Service.Infrastructure.System;

namespace RMS.Identity.Service.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddSingleton<MySqlConnectionFactory>();
        services.AddSingleton<DbSessionAccessor>();

        services.AddScoped<IUnitOfWork, MySqlUnitOfWork>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ISecureTokenService, SecureTokenService>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }
}
