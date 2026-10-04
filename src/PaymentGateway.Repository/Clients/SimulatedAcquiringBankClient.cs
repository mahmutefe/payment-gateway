using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using PaymentGateway.Domain.Dtos;
using PaymentGateway.Domain.Interfaces;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Repository.Clients;

public class SimulatedAcquiringBankClient(
    HttpClient httpClient,
    ILogger<SimulatedAcquiringBankClient> logger)
    : IAcquiringBankClient
{
    public async Task<BankPaymentResult> ProcessPaymentAsync(PostPaymentRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bankRequest = new
        {
            card_number = request.CardNumber,
            expiry_date = string.Create(
                CultureInfo.InvariantCulture,
                $"{request.ExpiryMonth:D2}/{request.ExpiryYear:D4}"),
            currency = request.Currency,
            amount = request.Amount,
            cvv = request.Cvv
        };

        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Bank payment request started.");

        try
        {
            using var response = await httpClient.PostAsJsonAsync("payments", bankRequest, cancellationToken);

            logger.LogInformation("Bank returned HTTP {BankStatusCode}", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();

            var bankResponse = await response.Content.ReadFromJsonAsync<BankResponse>( cancellationToken: cancellationToken)
                ?? throw new JsonException("Bank response was empty.");

            logger.LogInformation("Bank payment result received. Authorized: {Authorized}", bankResponse.Authorized);

            return new BankPaymentResult(bankResponse.Authorized, bankResponse.AuthorizationCode);
        }
        finally
        {
            logger.LogInformation("Bank payment call finished. ElapsedMs: {ElapsedMs}", stopwatch.Elapsed.TotalMilliseconds);
        }
    }
    
    private sealed class BankResponse
    {
        [JsonPropertyName("authorized")]
        public required bool Authorized { get; init; }

        [JsonPropertyName("authorization_code")]
        public string? AuthorizationCode { get; init; }
    }
}