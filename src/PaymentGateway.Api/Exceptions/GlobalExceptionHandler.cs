using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace PaymentGateway.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (context.RequestAborted.IsCancellationRequested)
            return false;

        var (status, title) = exception switch
        {
            ValidationException =>
                (400, "Request validation failed."),

            TimeoutRejectedException =>
                (504, "The bank did not respond in time."),

            BrokenCircuitException =>
                (503, "The bank is temporarily unavailable."),

            HttpRequestException bankError
                when bankError.StatusCode is null ||
                     (int)bankError.StatusCode.Value >= 500 =>
                (503, "The bank is temporarily unavailable."),

            HttpRequestException =>
                (502, "The bank returned an unexpected response."),

            JsonException =>
                (502, "The bank returned an invalid response."),

            _ =>
                (500, "An unexpected error occurred.")
        };

        if (status >= 500)
        {
            logger.LogError(
                exception,
                "Request failed with status {StatusCode}",
                status);
        }
        else
        {
            logger.LogWarning(
                "Request rejected. ErrorType: {ErrorType}, Status: {StatusCode}",
                exception.GetType().Name,
                status);
        }

        await Results.Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?>
            {
                ["correlationId"] = context.TraceIdentifier
            }).ExecuteAsync(context);

        return true;
    }
}