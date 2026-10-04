using PaymentGateway.Domain.Dtos;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Interfaces;

public interface IAcquiringBankClient
{
    Task<BankPaymentResult> ProcessPaymentAsync(PostPaymentRequestDto request, CancellationToken cancellationToken);
}