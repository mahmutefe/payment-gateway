using System.Text.Json.Serialization;

namespace PaymentGateway.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<PaymentStatus>))]
public enum PaymentStatus
{
    Authorized,
    Declined,
    Rejected
}