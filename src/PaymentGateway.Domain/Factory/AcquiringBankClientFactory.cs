using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Domain.Interfaces;

namespace PaymentGateway.Domain.Factory;

public sealed class AcquiringBankClientFactory(IServiceProvider provider)
    : IAcquiringBankClientFactory
{
    public IAcquiringBankClient GetClient(string bankKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankKey);

        return provider.GetRequiredKeyedService<IAcquiringBankClient>(
            bankKey);
    }
}