using System.Net;
using System.Text;
using System.Text.Json;

namespace PaymentGateway.Api.Tests.Infrastructure;

internal sealed class FakeBankHandler : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public bool Authorized { get; set; } = true;
    public bool DelayResponse { get; set; }
    public int CallCount { get; private set; }
    public string? LastCorrelationId { get; private set; }
    public string? LastPath { get; private set; }
    public bool WasCancelled { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastPath = request.RequestUri?.AbsolutePath;

        LastCorrelationId = request.Headers.TryGetValues(
            "X-Correlation-ID", out var values)
            ? values.Single()
            : null;

        if (DelayResponse)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                WasCancelled = true;
                throw;
            }
        }

        var json = Status == HttpStatusCode.OK
            ? JsonSerializer.Serialize(new
            {
                authorized = Authorized,
                authorization_code = Authorized ? "test-code" : ""
            })
            : "{}";

        return new HttpResponseMessage(Status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}