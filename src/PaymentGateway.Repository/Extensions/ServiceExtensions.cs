using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Domain.Interfaces;
using PaymentGateway.Repository.Clients;
using PaymentGateway.Repository.Repositories;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Timeout;

namespace PaymentGateway.Repository.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositoryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Bank:BaseUrl"]
                      ?? throw new InvalidOperationException(
                          "Bank base URL is not configured.");

        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

        services.AddHttpClient("simulator", client =>
        {
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = Timeout.InfiniteTimeSpan;
        }).AddResilienceHandler("bank", pipeline =>
        {
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),

                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => (int)response.StatusCode >= 500)
            });

            pipeline.AddTimeout(TimeSpan.FromSeconds(10));
        });
        
        services.AddKeyedScoped<IAcquiringBankClient>(
            "simulator",
            (provider, _) => new SimulatedAcquiringBankClient(
                provider.GetRequiredService<IHttpClientFactory>()
                    .CreateClient("simulator"),
                provider.GetRequiredService<
                    ILogger<SimulatedAcquiringBankClient>>()));
        
        return services;
    }
}