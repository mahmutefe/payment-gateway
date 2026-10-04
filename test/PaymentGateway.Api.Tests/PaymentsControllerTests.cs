using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Tests.Infrastructure;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Interfaces;
using PaymentGateway.Domain.Models;
using Shouldly;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests : IDisposable
{
    private readonly PaymentGatewayApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IPaymentRepository _repository;

    public PaymentsControllerTests()
    {
        _factory = new PaymentGatewayApplicationFactory();
        _client = _factory.CreateGatewayClient();

        _repository = _factory.Services
            .GetRequiredService<IPaymentRepository>();
    }

    [Fact]
    public async Task When_PaymentExists_Should_ReturnPayment()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "0012",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Amount = 1050,
            Currency = "GBP"
        };

        _repository.Add(payment, CancellationToken.None);

        // Act
        using var response = await _client.GetAsync($"/api/payments/{payment.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var retrieved = (await response.Content.ReadFromJsonAsync<GetPaymentResponse>()).ShouldNotBeNull();
        retrieved.Id.ShouldBe(payment.Id);
        retrieved.Status.ShouldBe(payment.Status);
        retrieved.CardNumberLastFour.ShouldBe(payment.CardNumberLastFour);
        retrieved.ExpiryMonth.ShouldBe(payment.ExpiryMonth);
        retrieved.ExpiryYear.ShouldBe(payment.ExpiryYear);
        retrieved.Amount.ShouldBe(payment.Amount);
        retrieved.Currency.ShouldBe(payment.Currency);
    }

    [Fact]
    public async Task When_PaymentDoesNotExist_Should_Return404()
    {
        // Arrange
        var missingPaymentId = Guid.NewGuid();

        // Act
        using var response = await _client.GetAsync($"/api/payments/{missingPaymentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    
    public void Dispose()
    {
        _client.Dispose();
        _factory.DisposeAsync();
    }
}