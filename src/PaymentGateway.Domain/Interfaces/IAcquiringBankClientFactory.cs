namespace PaymentGateway.Domain.Interfaces;

public interface IAcquiringBankClientFactory
{
    IAcquiringBankClient GetClient(string bankKey);
}