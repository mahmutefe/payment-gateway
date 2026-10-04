using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validators;

public class PostPaymentRequestValidator : IValidator<PostPaymentRequest>
{
    public ValidatorResult Validate(PostPaymentRequest? request)
    {
        var errors = new List<string>();

        if (request is null)
        {
            return new ValidatorResult
            {
                IsValid = false,
                Errors = ["Payment request is required."]
            };
        }


        
        if (string.IsNullOrEmpty(request.CardNumber))
        {
            errors.Add("Card number is required.");
        }
        
        if (string.IsNullOrEmpty(request.Cvv))
        {
            errors.Add("CVV is required.");
        }

        if (!string.IsNullOrEmpty(request.CardNumber) && !ContainsOnlyDigits(request.CardNumber, 14, 19))
        {
            errors.Add("Card number must contain between 14 and 19 digits."); 
        }

        if (request.ExpiryMonth is < 1 or > 12)
        {
            errors.Add("Expiry month must be between 1 and 12.");
        }

        var today = DateTime.UtcNow;

        if (request.ExpiryYear < today.Year ||
            request.ExpiryYear > 9999 ||
            (request.ExpiryYear == today.Year &&
             request.ExpiryMonth <= today.Month))
        {
            errors.Add("Expiry month and year must be in the future.");
        }

        if (string.IsNullOrEmpty(request.Currency))
        {
            errors.Add("Currency is required.");
        }
        
        if (!string.IsNullOrEmpty(request.Currency) && request.Currency is not ("GBP" or "USD" or "EUR"))
        {
            errors.Add("Currency must be GBP, USD or EUR.");
        }

        if (request.Amount <= 0)
        {
            errors.Add("Amount must be greater than zero.");
        }

        if (!string.IsNullOrEmpty(request.Cvv) && !ContainsOnlyDigits(request.Cvv, 3, 4))
        {
            errors.Add("CVV must contain 3 or 4 digits.");
        }

        return new ValidatorResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private static bool ContainsOnlyDigits(
        string? value, int minLength, int maxLength)
    {
        if (value is null ||
            value.Length < minLength ||
            value.Length > maxLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is < '0' or > '9')
                return false;
        }

        return true;
    }
}