using RMS.Identity.Service.Application.Commands.Companies;
using Microsoft.Extensions.DependencyInjection;
using RMS.Identity.Service.Application.Commands.Login;
using RMS.Identity.Service.Application.Commands.Refresh;
using RMS.Identity.Service.Application.Commands.SignUp;
using RMS.Identity.Service.Domain.Contracts.Login;
using RMS.Identity.Service.Domain.Contracts.Companies;
using RMS.Identity.Service.Domain.Contracts.CompanyUsers;
using RMS.Identity.Service.Domain.Contracts.Refresh;
using RMS.Identity.Service.Domain.Contracts.SignUp;
using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<LoginCommandRequest, LoginCommandResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<CreateCompanyUserCommandRequest, CreateCompanyUserCommandResponse>, CreateCompanyUserCommandHandler>();
        services.AddScoped<ICommandHandler<GetCompanyCommandRequest, GetCompanyCommandResponse>, GetCompanyCommandHandler>();
        services.AddScoped<ICommandHandler<GetCompanyUserCommandRequest, GetCompanyUserCommandResponse>, GetCompanyUserCommandHandler>();
        services.AddScoped<ICommandHandler<GetCurrentUserCompaniesCommandRequest, GetCurrentUserCompaniesCommandResponse>, GetCurrentUserCompaniesCommandHandler>();
        services.AddScoped<ICommandHandler<RegisterCompanyCommandRequest, RegisterCompanyCommandResponse>, RegisterCompanyCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshCommandRequest, RefreshCommandResponse>, RefreshCommandHandler>();
        services.AddScoped<ICommandHandler<SignUpCommandRequest, SignUpCommandResponse>, SignUpCommandHandler>();
        return services;
    }
}
