using Microsoft.Extensions.DependencyInjection;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public static class IdempotencyServiceCollectionExtensions
{
    public static IServiceCollection AddIdempotencyMiddlewareSupport(this IServiceCollection services)
    {
        services.AddScoped<IIdempotencyRequestFactory, IdempotencyRequestFactory>();

        return services;
    }
}
