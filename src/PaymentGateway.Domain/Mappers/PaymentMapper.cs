using PaymentGateway.Domain.Dtos;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Mappers;

public static class PaymentMapper
{
    public static Payment ToPayment(
        PostPaymentRequestDto request,
        BankPaymentResult bankResult)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(bankResult);

        return new Payment
        {
            Id = Guid.NewGuid(),
            Status = bankResult.Authorized
                ? PaymentStatus.Authorized
                : PaymentStatus.Declined,
            CardNumberLastFour = request.CardNumber[^4..],
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount
        };
    }
    
    public static PostPaymentResponseDto ToResponseDto(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new PostPaymentResponseDto
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        };
    }
}