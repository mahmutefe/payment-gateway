using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Interfaces;

public interface IPaymentRepository
{
    void Add(Payment payment, CancellationToken cancellationToken);
    Payment? GetById(Guid id, CancellationToken cancellationToken);
}