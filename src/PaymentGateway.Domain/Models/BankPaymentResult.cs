namespace PaymentGateway.Domain.Models;

public sealed record BankPaymentResult(
    bool Authorized,
    string? AuthorizationCode);