# Payment Gateway

A .NET 8 payment gateway built for the Checkout.com payment gateway challenge. It validates payment requests, submits valid payments to the supplied acquiring bank simulator, stores the result in memory, and retrieves payments by ID.

The implementation keeps the payment flow small while providing structured logging, correlation IDs, centralized error responses, and HTTP timeout and circuit breaker protection.

## Quick start

Prerequisites: the .NET 8 SDK, Docker with Docker Compose, and an available local HTTPS development certificate.

Run the following commands from the repository root.

```bash
# Start the supplied bank simulator.
docker compose up -d

# Trust the development certificate if needed.
dotnet dev-certs https --trust

# Start the API locally in Development.
dotnet run --project src/PaymentGateway.Api --launch-profile PaymentGateway.Api
```

| Component | Address |
|---|---|
| Gateway API | https://localhost:7092 |
| Swagger UI | https://localhost:7092/swagger |
| Bank simulator | http://localhost:8080/payments |
| Mountebank administration | http://localhost:2525 |

Docker Compose starts the bank simulator; the API runs separately through the .NET CLI or Rider. Swagger is enabled in Development only.

### Optional Seq logging

To start the simulator and Seq together:

```bash
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d
```

Open Seq at http://localhost:5341. The API sends logs there when `Seq:Enabled` is true; this setting is enabled in `appsettings.Development.json`. To run without Seq, set it to false or use an environment override:

```bash
Seq__Enabled=false dotnet run --project src/PaymentGateway.Api --launch-profile PaymentGateway.Api
```

The optional Seq container binds to loopback and disables authentication for local development. Its events persist in the `seq-data` Docker volume. It is not a production deployment configuration. The supplied simulator configuration and `.editorconfig` are retained.

## API

### Create a payment

`POST /api/payments`

```bash
curl -i https://localhost:7092/api/payments \
  -H 'Content-Type: application/json' \
  -d '{
    "cardNumber": "2222405343240013",
    "expiryMonth": 12,
    "expiryYear": 2030,
    "currency": "GBP",
    "amount": 1050,
    "cvv": "123"
  }'
```

Use a future expiry date when running the example. Amounts are integers in minor currency units: `1050` represents GBP 10.50.

A completed bank decision creates a payment record and returns `201 Created`, including when the bank declines the payment. The `Location` header identifies the GET endpoint for the new record.

Example response (the ID is generated per payment):

```json
{
  "id": "3870f1b0-e274-4e90-8fb6-19f04faebbf1",
  "status": "Authorized",
  "cardNumberLastFour": "0013",
  "expiryMonth": 12,
  "expiryYear": 2030,
  "currency": "GBP",
  "amount": 1050
}
```

### Retrieve a payment

`GET /api/payments/{id}`

```bash
curl -i https://localhost:7092/api/payments/3870f1b0-e274-4e90-8fb6-19f04faebbf1
```

Replace the example ID with one returned by POST. Existing payments return `200 OK` with the same payment fields. Unknown IDs return `404 Not Found`. Retrieval does not call the bank.

### Validation

Invalid requests return `400 Bad Request` before the bank is called.

| Field | Rule |
|---|---|
| `cardNumber` | Required string containing 14–19 ASCII digits |
| `expiryMonth` | Integer from 1 to 12 |
| `expiryYear` | Combined month and year must be later than the current UTC month; year cannot exceed 9999 |
| `currency` | Required, case-sensitive `GBP`, `USD`, or `EUR` |
| `amount` | Positive integer in minor units |
| `cvv` | Required string containing 3 or 4 ASCII digits |

Card numbers, CVVs, and the last four digits are strings to preserve leading zeroes. The validator collects errors rather than throwing exceptions for invalid input. Malformed JSON is rejected by ASP.NET Core model binding.

### Response codes

| Code | Meaning |
|---|---|
| `201` | Payment created with an `Authorized` or `Declined` decision |
| `200` | Stored payment retrieved |
| `400` | Invalid request |
| `404` | Payment not found |
| `500` | Unexpected application error |
| `502` | Unexpected bank HTTP response or invalid bank JSON |
| `503` | Bank unavailable, connection failure, or open circuit |
| `504` | Bank HTTP operation timed out |

Validation and centrally handled exceptions use Problem Details JSON. Validation responses also contain an `errors` collection. Error responses include `correlationId`; the response header `X-Correlation-ID` contains the same request identifier. Swagger declares the success and error response types.

## Architecture

```text
src/
  PaymentGateway.Api/          HTTP endpoints, validation, API mapping, error handling
  PaymentGateway.Domain/       Payment flow, models, DTOs, interfaces, bank client factory
  PaymentGateway.Repository/   In-memory repository and simulator HTTP client
test/
  PaymentGateway.Api.Tests/    Unit and HTTP integration tests
imposters/                    Supplied bank simulator configuration
```

The API references Domain and Repository. Repository references Domain and implements its interfaces; Domain has no dependency on Repository. API startup registers the implementations through service extensions.

The payment flow is:

1. Validate the incoming request.
2. Map it to the service request DTO.
3. Resolve the bank client and await its response.
4. Map the bank decision to a payment, retaining only the last four card digits.
5. Store the payment and return its response.

The repository is a singleton backed by `ConcurrentDictionary`. It supports adding and looking up payments and rejects duplicate IDs without overwriting an existing record.

The bank client uses a named `HttpClient` and a keyed service registration. A small factory resolves the client by key. The service currently selects `simulator`; additional banks would require another client registration and a bank-selection policy. Dynamic routing is not implemented.

### Resilience

The HTTP pipeline uses Polly through `Microsoft.Extensions.Http.Resilience`:

- A 10-second timeout, with the separate `HttpClient.Timeout` disabled.
- A circuit breaker with a 50% failure ratio, a minimum of 5 requests in a 30-second sampling window, and a 30-second break duration.
- HTTP 5xx responses, connection exceptions, and Polly timeouts count as circuit failures. A normal declined payment does not.
- No automatic retries, to avoid repeating a payment submission after an uncertain outcome.

Technical bank failures do not create an `Authorized` or `Declined` record.

## Observability

The API generates a fresh correlation ID for every request. It does not reuse an incoming correlation header. The ID is returned in `X-Correlation-ID`, placed in the logging scope, and forwarded to the bank using the same header.

Structured logs include payment outcomes, bank HTTP status and elapsed time, and request context. The scope also contains the directly connected client's IP address. The application explicitly logs only the last four card digits; it does not log the full request body, full card number, CVV, or authorization code.

Console logging remains enabled. Optional Seq logging uses the standard .NET logging provider. In Seq, filter an individual request with:

```text
CorrelationId = 'value-from-the-response-header'
```

This is correlated logging, not a full distributed tracing or metrics implementation.

## Tests and coverage

```bash
dotnet build PaymentGateway.sln
dotnet test PaymentGateway.sln

# Run tests and collect coverage.
dotnet test PaymentGateway.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Tests use xUnit, Shouldly assertions, and Moq for service dependencies. HTTP integration tests use `WebApplicationFactory` and a configurable fake bank HTTP handler while retaining the actual gateway HTTP client, resilience pipeline, and exception handling. Docker and Seq are not required for automated tests.

Coverage includes validation, service mapping and persistence, repository behavior, authorized and declined POST/GET flows, unavailable-bank responses, timeout cancellation, correlation propagation, and open-circuit request blocking. The timeout integration test uses the configured 10-second timeout.

Last verified run on 4 October 2026: **37 passed, 0 failed, 0 skipped**.

| Assembly | Line coverage | Branch coverage |
|---|---:|---:|
| API | 93.85% | 84.69% |
| Domain | 96.59% | 100% |
| Repository | 97.46% | 62.50% |
| **Total** | **95%** | **83.63%** |

The collector writes `coverage.cobertura.xml` in a generated subdirectory under `TestResults`; the test output prints its location. These figures describe the verified run, not an enforced coverage threshold or a guarantee of correctness.

### Manual simulator scenarios

Keep all fields valid and vary the final card digit:

| Final digit | Simulator decision | Expected gateway response |
|---|---|---|
| `1`, `3`, `5`, `7`, `9` | Authorized | `201`, `Authorized`; GET returns the stored decision |
| `2`, `4`, `6`, `8` | Declined | `201`, `Declined`; GET returns the stored decision |
| `0` | HTTP 503 | `503` Problem Details; no payment record |

For validation checks, omit the card number, use a past expiry date, or send `TRY` as the currency. Expect `400` without a bank call. Timeout and circuit breaker behavior are also covered by automated integration tests without changing the supplied simulator configuration.

## Assumptions and limits

- This is a challenge implementation. Authentication, merchant ownership checks, and production deployment hardening are outside its scope.
- Payments exist only in the current process and are lost on restart. Multiple gateway instances do not share storage.
- Invalid input is rejected with `400` rather than persisted as a `Rejected` payment. Only completed bank decisions are stored.
- Amounts must be greater than zero, and expiry must be strictly later than the current month. These are explicit validation choices.
- There is no idempotency contract. Repeating a POST can submit another payment to the bank.
- A timeout can leave the bank's outcome unknown. The gateway does not automatically retry, reconcile, or reverse payments.
- Bank authorization and local storage are not atomic. If saving fails after authorization, the API returns a technical error; the bank decision is not rolled back.
- The bank authorization code is read from the response but is not persisted or exposed by this API.

## Challenge reference

Based on the [Checkout.com payment gateway challenge](https://github.com/cko-recruitment) and its [.NET starter](https://github.com/cko-recruitment/payment-gateway-challenge-dotnet). The starter permits solution restructuring and alternative test libraries; the supplied `imposters/` configuration and `.editorconfig` should not be changed.
