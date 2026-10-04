namespace PaymentGateway.Api.Validators;

public interface IValidator<in T > where T : class
{
    ValidatorResult Validate(T request);
}