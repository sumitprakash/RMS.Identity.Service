using RMS.Identity.Service.Tests.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace RMS.Identity.Service.Tests.Endpoint.SignUp;

public sealed class SignUpWebApplicationFactory : TestDatabaseWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailVerification:AutoVerifyOnSignUp"] = "false"
            });
        });
    }
}
