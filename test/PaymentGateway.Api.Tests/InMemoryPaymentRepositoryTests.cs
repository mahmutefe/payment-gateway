using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;
using PaymentGateway.Repository.Repositories;
using Shouldly;

namespace PaymentGateway.Api.Tests;

public class InMemoryPaymentRepositoryTests
{
    private readonly InMemoryPaymentRepository _repository = new();

    [Fact]
    public void When_PaymentIsAdded_Should_ReturnStoredPayment()
    {
        // Arrange
        var payment = CreatePayment(Guid.NewGuid());

        _repository.Add(payment, CancellationToken.None);

        // Act
        var retrieved = _repository.GetById(payment.Id, CancellationToken.None);

        // Assert
        retrieved.ShouldBe(payment);
    }

    [Fact]
    public void When_PaymentDoesNotExist_Should_ReturnNull()
    {
        // Arrange & Act
        var retrieved = _repository.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        retrieved.ShouldBeNull();
    }

    [Fact]
    public void When_PaymentIdAlreadyExists_Should_RejectDuplicateAndKeepOriginal()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = CreatePayment(id, amount: 1050);
        var duplicate = CreatePayment(id, amount: 2000);

        _repository.Add(original, CancellationToken.None);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _repository.Add(duplicate, CancellationToken.None));

        var retrieved = _repository.GetById(id, CancellationToken.None);
        retrieved.ShouldBe(original);
        retrieved.ShouldNotBeNull().Amount.ShouldBe(1050);
    }

    private static Payment CreatePayment(
        Guid id,
        int amount = 1050) => new()
    {
        Id = id,
        Status = PaymentStatus.Authorized,
        CardNumberLastFour = "0013",
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = amount
    };
}