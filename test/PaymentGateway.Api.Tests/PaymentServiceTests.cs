using Moq;
using PaymentGateway.Domain.Dtos;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Interfaces;
using PaymentGateway.Domain.Models;
using PaymentGateway.Domain.Services;
using Shouldly;

namespace PaymentGateway.Api.Tests;

public class PaymentServiceTests
{
    private readonly Mock<IAcquiringBankClient> _bank = new();
    private readonly Mock<IPaymentRepository> _repository = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        var factory = new Mock<IAcquiringBankClientFactory>();
        factory
            .Setup(x => x.GetClient("simulator"))
            .Returns(_bank.Object);

        _service = new PaymentService(factory.Object, _repository.Object);
    }

    [Theory]
    [InlineData(true, PaymentStatus.Authorized)]
    [InlineData(false, PaymentStatus.Declined)]
    public async Task When_BankReturnsResult_Should_SavePaymentAndReturnResponse(
        bool authorized,
        PaymentStatus expectedStatus)
    {
        // Arrange
        var request = ValidRequest();
        var token = CancellationToken.None;
        Payment? savedPayment = null;

        _bank
            .Setup(x => x.ProcessPaymentAsync(request, token))
            .ReturnsAsync(new BankPaymentResult(authorized, null));

        _repository
            .Setup(x => x.Add(It.IsAny<Payment>(), token))
            .Callback<Payment, CancellationToken>(
                (payment, _) => savedPayment = payment);

        // Act
        var response = (await _service.ProcessPaymentAsync(request, token)).ShouldNotBeNull();

        // Assert
        var saved = savedPayment.ShouldNotBeNull();

        saved.Id.ShouldNotBe(Guid.Empty);
        saved.Status.ShouldBe(expectedStatus);
        saved.CardNumberLastFour.ShouldBe("0013");
        saved.Amount.ShouldBe(request.Amount);
        saved.Currency.ShouldBe(request.Currency);
        saved.ExpiryMonth.ShouldBe(request.ExpiryMonth);
        saved.ExpiryYear.ShouldBe(request.ExpiryYear);

        response.Id.ShouldBe(saved.Id);
        response.Status.ShouldBe(saved.Status);
        response.CardNumberLastFour.ShouldBe(saved.CardNumberLastFour);
        response.Amount.ShouldBe(saved.Amount);
        response.Currency.ShouldBe(saved.Currency);
        response.ExpiryMonth.ShouldBe(saved.ExpiryMonth);
        response.ExpiryYear.ShouldBe(saved.ExpiryYear);

        _bank.Verify(
            x => x.ProcessPaymentAsync(request, token),
            Times.Once);

        _repository.Verify(
            x => x.Add(It.IsAny<Payment>(), token),
            Times.Once);
    }

    [Fact]
    public async Task When_BankFails_Should_NotSavePayment()
    {
        // Arrange
        var request = ValidRequest();
        var token = CancellationToken.None;
        var expectedError = new HttpRequestException("Bank is unavailable.");

        _bank
            .Setup(x => x.ProcessPaymentAsync(request, token))
            .ThrowsAsync(expectedError);

        // Act & Assert
        var error = await Should.ThrowAsync<HttpRequestException>(
            async () =>
            {
                await _service.ProcessPaymentAsync(request, token);
            });

        error.ShouldBeSameAs(expectedError);

        _bank.Verify(
            x => x.ProcessPaymentAsync(request, token),
            Times.Once);

        _repository.Verify(
            x => x.Add(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task When_SavingFails_Should_NotCallBankAgain()
    {
        // Arrange
        var request = ValidRequest();
        var token = CancellationToken.None;
        var expectedError = new InvalidOperationException("Saving failed.");

        _bank.Setup(x => x.ProcessPaymentAsync(request, token))
            .ReturnsAsync(new BankPaymentResult(true, null));

        _repository.Setup(x => x.Add(It.IsAny<Payment>(), token))
            .Throws(expectedError);

        // Act & Assert
        var error = await Should.ThrowAsync<InvalidOperationException>(
            async () =>
            {
                await _service.ProcessPaymentAsync(request, token);
            });

        error.ShouldBeSameAs(expectedError);

        _bank.Verify(
            x => x.ProcessPaymentAsync(request, token),
            Times.Once);

        _repository.Verify(
            x => x.Add(It.IsAny<Payment>(), token),
            Times.Once);
    }

    private static PostPaymentRequestDto ValidRequest() => new()
    {
        CardNumber = "2222405343240013",
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = 1050,
        Cvv = "123"
    };
}