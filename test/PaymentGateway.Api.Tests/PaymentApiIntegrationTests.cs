using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Tests.Infrastructure;
using PaymentGateway.Domain.Enums;
using Shouldly;

namespace PaymentGateway.Api.Tests;

public class PaymentApiIntegrationTests : IDisposable
{
    private readonly FakeBankHandler _bank;
    private readonly PaymentGatewayApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentApiIntegrationTests()
    {
        _bank = new FakeBankHandler();
        _factory = new PaymentGatewayApplicationFactory(_bank);
        _client = _factory.CreateGatewayClient();
    }
    
    [Fact]
    public async Task When_BankAuthorizesPayment_Should_CreateRetrievablePayment()
    {
        // Arrange
        _bank.Authorized = true;
        _bank.Status = HttpStatusCode.OK;
        
        var request = ValidRequest();

        // Act
        using var post = await _client.PostAsJsonAsync("/api/payments", request);

        // Assert
        post.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = post.Headers.Location.ShouldNotBeNull();

        var created = (await post.Content.ReadFromJsonAsync<PostPaymentResponse>()).ShouldNotBeNull();

        created.Id.ShouldNotBe(Guid.Empty);
        created.Status.ShouldBe(PaymentStatus.Authorized);
        created.CardNumberLastFour.ShouldBe("0013");
        created.Amount.ShouldBe(request.Amount);
        created.Currency.ShouldBe(request.Currency);
        created.ExpiryMonth.ShouldBe(request.ExpiryMonth);
        created.ExpiryYear.ShouldBe(request.ExpiryYear);

        var correlationId = post.Headers.GetValues("X-Correlation-ID").Single();

        correlationId.ShouldNotBeNullOrWhiteSpace();
        _bank.LastCorrelationId.ShouldBe(correlationId);
        _bank.LastPath.ShouldBe("/payments");

        using var get = await _client.GetAsync(location);

        get.StatusCode.ShouldBe(HttpStatusCode.OK);

        var retrieved = (await get.Content.ReadFromJsonAsync<GetPaymentResponse>()).ShouldNotBeNull();
        retrieved.Id.ShouldBe(created.Id);
        retrieved.Status.ShouldBe(created.Status);
        retrieved.CardNumberLastFour.ShouldBe(created.CardNumberLastFour);
        retrieved.ExpiryMonth.ShouldBe(created.ExpiryMonth);
        retrieved.ExpiryYear.ShouldBe(created.ExpiryYear);
        retrieved.Currency.ShouldBe(created.Currency);
        retrieved.Amount.ShouldBe(created.Amount);

        _bank.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task When_BankIsUnavailable_Should_Return503WithCorrelationId()
    {
        // Arrange
        _bank.Status = HttpStatusCode.ServiceUnavailable;

        // Act
        using var response = await _client.PostAsJsonAsync("/api/payments", ValidRequest());

        // Assert
        response.StatusCode.ShouldBe( HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var correlationId = response.Headers.GetValues("X-Correlation-ID").Single();
        correlationId.ShouldNotBeNullOrWhiteSpace();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(503);
        body.RootElement.GetProperty("correlationId").GetString().ShouldBe(correlationId);
        _bank.LastCorrelationId.ShouldBe(correlationId);
        _bank.CallCount.ShouldBe(1);
        response.Headers.Location.ShouldBeNull();
    }

    [Fact]
    public async Task When_BankTimesOut_Should_Return504WithCorrelationId()
    {
        // Arrange
        _bank.DelayResponse = true;
        
        // Act
        using var response = await _client.PostAsJsonAsync("/api/payments", ValidRequest());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var correlationId = response.Headers.GetValues("X-Correlation-ID").Single();
        correlationId.ShouldNotBeNullOrWhiteSpace();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(504);
        body.RootElement.GetProperty("correlationId").GetString().ShouldBe(correlationId);
        _bank.LastCorrelationId.ShouldBe(correlationId);
        _bank.CallCount.ShouldBe(1);
        _bank.WasCancelled.ShouldBeTrue();
        response.Headers.Location.ShouldBeNull();
    }
    
    [Fact]
    public async Task When_BankDeclinesPayment_Should_CreateRetrievablePayment()
    {
        // Arrange
        _bank.Authorized = false;

        // Act
        using var post = await _client.PostAsJsonAsync("/api/payments", ValidRequest());

        // Assert
        post.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = post.Headers.Location.ShouldNotBeNull();
        var created = (await post.Content.ReadFromJsonAsync<PostPaymentResponse>()).ShouldNotBeNull();
        created.Status.ShouldBe(PaymentStatus.Declined);
        created.CardNumberLastFour.ShouldBe("0013");

        using var get = await _client.GetAsync(location);
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        var retrieved = (await get.Content.ReadFromJsonAsync<GetPaymentResponse>()).ShouldNotBeNull();
        retrieved.Id.ShouldBe(created.Id);
        retrieved.Status.ShouldBe(PaymentStatus.Declined);
        _bank.CallCount.ShouldBe(1);
    }
    
    [Fact]
    public async Task When_CircuitIsOpen_Should_StopCallingBank()
    {
        // Arrange
        _bank.Status = HttpStatusCode.ServiceUnavailable;

        // Act & Assert
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await _client.PostAsJsonAsync("/api/payments", ValidRequest());
            failed.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        _bank.CallCount.ShouldBe(5);

        using var blocked = await _client.PostAsJsonAsync("/api/payments", ValidRequest());
        blocked.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        _bank.CallCount.ShouldBe(5);
        blocked.Headers.Location.ShouldBeNull();

        var correlationId = blocked.Headers.GetValues("X-Correlation-ID").Single();
        correlationId.ShouldNotBeNullOrWhiteSpace();

        using var body = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(503);
        body.RootElement.GetProperty("correlationId").GetString().ShouldBe(correlationId);
    }
    
    [Fact]
    public async Task When_CardNumberIsMissing_Should_Return400WithoutCallingBank()
    {
        // Arrange
        _bank.Authorized = true;

        var request = ValidRequest();
        request.CardNumber = null;

        // Act
        using var response = await _client.PostAsJsonAsync("/api/payments", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        _bank.CallCount.ShouldBe(0);

        var correlationId = response.Headers .GetValues("X-Correlation-ID").Single();
        correlationId.ShouldNotBeNullOrWhiteSpace();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        body.RootElement.GetProperty("correlationId").GetString().ShouldBe(correlationId);
        body.RootElement.TryGetProperty("traceId", out _).ShouldBeFalse();
        body.RootElement.GetProperty("errors")
            .GetProperty("payment")[0].GetString()
            .ShouldBe("Card number is required.");
    }
    
    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumber = "2222405343240013",
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = 1050,
        Cvv = "123"
    };
    
    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}