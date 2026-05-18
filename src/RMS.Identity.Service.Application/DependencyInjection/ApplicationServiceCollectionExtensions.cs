using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.Commands.SignUp;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<SignUpCommandRequest, SignUpCommandResponse>, SignUpCommandHandler>();
        return services;
    }
}
