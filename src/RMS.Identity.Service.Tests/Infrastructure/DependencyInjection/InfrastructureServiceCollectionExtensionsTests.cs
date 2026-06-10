using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Infrastructure.DependencyInjection;
using RMS.Identity.Service.Infrastructure.Security;

namespace RMS.Identity.Service.Tests.Infrastructure.DependencyInjection;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddIdentityServiceInfrastructure_WhenSigningKeyEnvVarIsMissing_KeepsConfiguredSigningKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=rms_identity;User ID=rms_user;Password=12345678;",
                ["Jwt:Issuer"] = "RMS.Identity.Service",
                ["Jwt:Audience"] = "RMS",
                ["Jwt:SigningKey"] = "replace-this-development-signing-key-with-a-secure-secret",
                ["Jwt:SigningKeyEnvVar"] = "JWT_SIGNING_KEY",
                ["Jwt:AccessTokenLifetimeSeconds"] = "3600",
                ["Jwt:RefreshTokenLifetimeDays"] = "30"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddIdentityServiceInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal("replace-this-development-signing-key-with-a-secure-secret", options.SigningKey);
    }

    [Fact]
    public void AddIdentityServiceInfrastructure_WhenSigningKeyEnvVarIsPresent_UsesEnvironmentSigningKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=3306;Database=rms_identity;User ID=rms_user;Password=12345678;",
                ["Jwt:Issuer"] = "RMS.Identity.Service",
                ["Jwt:Audience"] = "RMS",
                ["Jwt:SigningKey"] = "replace-this-development-signing-key-with-a-secure-secret",
                ["Jwt:SigningKeyEnvVar"] = "JWT_SIGNING_KEY",
                ["Jwt:AccessTokenLifetimeSeconds"] = "3600",
                ["Jwt:RefreshTokenLifetimeDays"] = "30",
                ["JWT_SIGNING_KEY"] = "environment-signing-key-with-at-least-thirty-two-bytes"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddIdentityServiceInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal("environment-signing-key-with-at-least-thirty-two-bytes", options.SigningKey);
    }
}
