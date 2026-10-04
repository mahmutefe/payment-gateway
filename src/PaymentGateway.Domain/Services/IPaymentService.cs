using PaymentGateway.Domain.Dtos;

namespace PaymentGateway.Domain.Services;

public interface IPaymentService
{
    Task<PostPaymentResponseDto?> ProcessPaymentAsync(PostPaymentRequestDto request, CancellationToken cancellationToken);
    PostPaymentResponseDto? GetPayment(Guid id, CancellationToken cancellationToken);
}