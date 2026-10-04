using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Api.Tests.Infrastructure;

public sealed class PaymentGatewayApplicationFactory(
    HttpMessageHandler? bankHandler = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (bankHandler is null)
            return;

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient("simulator")
                .ConfigurePrimaryHttpMessageHandler(() => bankHandler);
        });
    }

    public HttpClient CreateGatewayClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
}