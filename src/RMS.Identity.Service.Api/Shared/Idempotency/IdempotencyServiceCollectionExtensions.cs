using Microsoft.Extensions.DependencyInjection;

namespace RMS.Identity.Service.Api.Shared.Idempotency;

public static class IdempotencyServiceCollectionExtensions
{
    public static IServiceCollection AddIdempotencyMiddlewareSupport(this IServiceCollection services)
    {
        services.AddScoped<IIdempotencyRequestFactory, IdempotencyRequestFactory>();
        services.AddScoped<IIdempotencyResponseCapture, IdempotencyResponseCapture>();
        services.AddScoped<IIdempotencyTransactionPipeline, IdempotencyTransactionPipeline>();

        return services;
    }
}
