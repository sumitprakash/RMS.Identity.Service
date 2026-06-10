using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.DependencyInjection;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Infrastructure.Persistence.Companies;
using RMS.Identity.Service.Infrastructure.Persistence.CompanyUsers;
using RMS.Identity.Service.Infrastructure.Idempotency;
using RMS.Identity.Service.Infrastructure.Persistence.Auth;
using RMS.Identity.Service.Infrastructure.Persistence.AuditLog;
using RMS.Identity.Service.Infrastructure.Persistence.Outbox;
using RMS.Identity.Service.Infrastructure.Persistence.Roles;
using RMS.Identity.Service.Infrastructure.Persistence.UserAccounts;
using RMS.Identity.Service.Infrastructure.Persistence.VerifyEmail;
using RMS.Identity.Service.Infrastructure.Notifications;
using RMS.Identity.Service.Infrastructure.Outbox;
using RMS.Identity.Service.Infrastructure.Security;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.Notifications;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Auth;
using RMS.Identity.Service.Domain.Interfaces.Repositories.AuditLog;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Companies;
using RMS.Identity.Service.Domain.Interfaces.Repositories.CompanyUsers;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Idempotency;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Outbox;
using RMS.Identity.Service.Domain.Interfaces.Repositories.Roles;
using RMS.Identity.Service.Domain.Interfaces.Repositories.UserAccounts;
using RMS.Identity.Service.Domain.Interfaces.Repositories.VerifyEmail;
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
        services.AddOptions<EmailDeliveryOptions>()
            .Bind(configuration.GetSection(EmailDeliveryOptions.SectionName))
            .PostConfigure(options =>
            {
                if (bool.TryParse(configuration[EmailDeliveryOptions.AutoVerifyByEndpointEnvVar], out var autoVerifyByEndpoint))
                {
                    options.AutoVerifyByEndpoint = autoVerifyByEndpoint;
                }

                var verifyEmailEndpointUrl = configuration[EmailDeliveryOptions.VerifyEmailEndpointUrlEnvVar];
                if (!string.IsNullOrWhiteSpace(verifyEmailEndpointUrl))
                {
                    options.VerifyEmailEndpointUrl = verifyEmailEndpointUrl;
                }

                if (string.IsNullOrWhiteSpace(options.SmtpPasswordEnvVar))
                {
                    return;
                }

                options.SmtpPassword = configuration[options.SmtpPasswordEnvVar] ?? string.Empty;
            })
            .Validate(options => !options.Enabled || options.AutoVerifyByEndpoint || !string.IsNullOrWhiteSpace(options.FromAddress), "Email sender address is required when email delivery is enabled.")
            .Validate(options => !options.Enabled || options.AutoVerifyByEndpoint || !string.IsNullOrWhiteSpace(options.VerificationUrlTemplate), "Email verification URL template is required when email delivery is enabled.")
            .Validate(options => !options.Enabled || options.AutoVerifyByEndpoint || options.VerificationUrlTemplate.Contains("{token}", StringComparison.Ordinal), "Email verification URL template must contain the {token} placeholder.")
            .Validate(options => !options.Enabled || options.AutoVerifyByEndpoint || !string.IsNullOrWhiteSpace(options.SmtpHost), "SMTP host is required when email delivery is enabled.")
            .Validate(options => !options.AutoVerifyByEndpoint || Uri.TryCreate(options.VerifyEmailEndpointUrl, UriKind.Absolute, out _), "Email verification endpoint URL must be absolute when endpoint auto-verification is enabled.")
            .Validate(options => options.PollIntervalSeconds > 0, "Email outbox poll interval must be greater than zero.")
            .Validate(options => options.BatchSize > 0, "Email outbox batch size must be greater than zero.")
            .Validate(options => options.MaxRetries > 0, "Email outbox max retries must be greater than zero.")
            .Validate(options => options.RetryDelaySeconds > 0, "Email outbox retry delay must be greater than zero.")
            .Validate(options => options.ProcessingTimeoutSeconds > 0, "Email outbox processing timeout must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.SigningKeyEnvVar))
                {
                    return;
                }

                var signingKey = configuration[options.SigningKeyEnvVar];
                if (!string.IsNullOrWhiteSpace(signingKey))
                {
                    options.SigningKey = signingKey;
                }
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
        services.AddScoped<CompanyMySqlRepository>();
        services.AddScoped<ICompanyReadRepository>(
            provider => provider.GetRequiredService<CompanyMySqlRepository>());
        services.AddScoped<ICompanyWriteRepository>(
            provider => provider.GetRequiredService<CompanyMySqlRepository>());
        services.AddScoped<ICompanyMembershipReadRepository, CompanyMembershipMySqlRepository>();
        services.AddScoped<CompanyUserMySqlRepository>();
        services.AddScoped<ICompanyUserReadRepository>(
            provider => provider.GetRequiredService<CompanyUserMySqlRepository>());
        services.AddScoped<ICompanyUserWriteRepository>(
            provider => provider.GetRequiredService<CompanyUserMySqlRepository>());
        services.AddScoped<AuditLogMySqlRepository>();
        services.AddScoped<IAuditLogWriteRepository>(
            provider => provider.GetRequiredService<AuditLogMySqlRepository>());
        services.AddScoped<IOperationalRoleReadRepository, OperationalRoleMySqlRepository>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddHttpClient<IEmailVerificationEndpointClient, EmailVerificationEndpointClient>();
        services.AddScoped<EmailVerificationRequestedOutboxProcessor>();
        services.AddHostedService<EmailVerificationOutboxWorker>();
        services.AddScoped<OutboxMySqlRepository>();
        services.AddScoped<IOutboxWriteRepository>(
            provider => provider.GetRequiredService<OutboxMySqlRepository>());
        services.AddScoped<IOutboxProcessingRepository>(
            provider => provider.GetRequiredService<OutboxMySqlRepository>());
        services.AddScoped<EmailVerificationMySqlRepository>();
        services.AddScoped<IEmailVerificationReadRepository>(
            provider => provider.GetRequiredService<EmailVerificationMySqlRepository>());
        services.AddScoped<IEmailVerificationWriteRepository>(
            provider => provider.GetRequiredService<EmailVerificationMySqlRepository>());

        return services;
    }
}
