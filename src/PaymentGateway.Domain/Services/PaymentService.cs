using PaymentGateway.Domain.Dtos;
using PaymentGateway.Domain.Interfaces;
using PaymentGateway.Domain.Mappers;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Services;

public class PaymentService(
    IAcquiringBankClientFactory bankClientFactory,
    IPaymentRepository paymentRepository) : IPaymentService
{

    public async Task<PostPaymentResponseDto?> ProcessPaymentAsync(PostPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var bankResult = await SendToBankAsync(request, "simulator", cancellationToken);
        var payment = PaymentMapper.ToPayment(request, bankResult);
        paymentRepository.Add(payment, cancellationToken);
        
        return PaymentMapper.ToResponseDto(payment);
    }
    
    public PostPaymentResponseDto? GetPayment( Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payment = paymentRepository.GetById(id, cancellationToken);

        return payment is null
            ? null
            : PaymentMapper.ToResponseDto(payment);
    }
    
    private async Task<BankPaymentResult> SendToBankAsync(
        PostPaymentRequestDto request,
        string bankKey,
        CancellationToken cancellationToken)
    {
        var bankClient = bankClientFactory.GetClient(bankKey);

        return await bankClient.ProcessPaymentAsync(
            request,
            cancellationToken);
    }
}