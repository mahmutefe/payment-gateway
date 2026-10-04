using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain.Dtos;

namespace PaymentGateway.Api.Mappers;

public static class PaymentMapper
{
    public static PostPaymentResponse MapToPostPaymentResponse(PostPaymentResponseDto dto)
    {
        return new PostPaymentResponse
        {
            Id = dto.Id,
            Status = dto.Status,
            CardNumberLastFour = dto.CardNumberLastFour,
            ExpiryMonth = dto.ExpiryMonth,
            ExpiryYear = dto.ExpiryYear,
            Currency = dto.Currency,
            Amount = dto.Amount
        };
    }

    public static PostPaymentRequestDto MapToPostPaymentRequestDto(PostPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PostPaymentRequestDto
        {
            CardNumber = request.CardNumber
                         ?? throw new InvalidOperationException(
                             "Validated payment request has no card number."),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency
                       ?? throw new InvalidOperationException(
                           "Validated payment request has no currency."),
            Amount = request.Amount,
            Cvv = request.Cvv
                  ?? throw new InvalidOperationException(
                      "Validated payment request has no CVV.")
        };
    }

    public static GetPaymentResponse MapToGetPaymentResponse(
        PostPaymentResponseDto dto)
    {
        return new GetPaymentResponse
        {
            Id = dto.Id,
            Status = dto.Status,
            CardNumberLastFour = dto.CardNumberLastFour,
            ExpiryMonth = dto.ExpiryMonth,
            ExpiryYear = dto.ExpiryYear,
            Currency = dto.Currency,
            Amount = dto.Amount
        };
    }
}