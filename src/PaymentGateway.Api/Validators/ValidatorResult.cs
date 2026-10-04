namespace PaymentGateway.Api.Validators;

public record ValidatorResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();
}