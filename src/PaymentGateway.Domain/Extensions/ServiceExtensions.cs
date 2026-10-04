using PaymentGateway.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Domain.Factory;
using PaymentGateway.Domain.Interfaces;

namespace PaymentGateway.Domain.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAcquiringBankClientFactory, AcquiringBankClientFactory>();
        
        return services;
    }
}