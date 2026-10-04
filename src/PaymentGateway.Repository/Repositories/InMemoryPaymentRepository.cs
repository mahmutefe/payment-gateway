using System.Collections.Concurrent;
using PaymentGateway.Domain.Interfaces;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Repository.Repositories;

public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public void Add(Payment payment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (payment.Id == Guid.Empty)
            throw new ArgumentException(
                "Payment ID must not be empty.", nameof(payment));

        if (!_payments.TryAdd(payment.Id, payment))
            throw new InvalidOperationException(
                "A payment with this ID already exists.");
    }

    public Payment? GetById(Guid id, CancellationToken cancellationToken)
    {
        return _payments.GetValueOrDefault(id);
    }
}