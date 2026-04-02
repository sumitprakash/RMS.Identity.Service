using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RMS.Identity.Service.Domain.Interfaces.SignUp;

namespace RMS.Identity.Service.Tests.Endpoint.SignUp;

public sealed class SignUpWebApplicationFactory : WebApplicationFactory<Program>
{
    public StubSignUpService StubService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISignUpService>();
            services.AddSingleton(StubService);
            services.AddSingleton<ISignUpService>(StubService);
        });
    }
}
