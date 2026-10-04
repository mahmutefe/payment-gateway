using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Handlers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;
using PaymentGateway.Domain.Extensions;
using PaymentGateway.Repository.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped< IValidator<PostPaymentRequest>, PostPaymentRequestValidator>();
builder.Services.AddDomainServices();
builder.Services.AddRepositoryServices(builder.Configuration);

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.Remove("traceId");
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.AddHttpClient("simulator").AddHttpMessageHandler<CorrelationIdHandler>();

if (builder.Configuration.GetValue<bool>("Seq:Enabled"))
{
    builder.Logging.AddSeq(builder.Configuration.GetSection("Seq"));
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = Guid.NewGuid().ToString("N");

    context.TraceIdentifier = correlationId;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        return Task.CompletedTask;
    });

    using var scope = app.Logger.BeginScope(
        new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString()
        });

    await next(context);
});

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }