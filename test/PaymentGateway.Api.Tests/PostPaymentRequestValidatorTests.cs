using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;
using Shouldly;

namespace PaymentGateway.Api.Tests;

public class PostPaymentRequestValidatorTests
{
    private readonly PostPaymentRequestValidator _validator = new();

    [Theory]
    [InlineData("GBP", 14, 3)]
    [InlineData("USD", 19, 4)]
    [InlineData("EUR", 16, 3)]
    public void When_AllFieldsAreValid_Should_AcceptRequest(
        string currency, int cardLength, int cvvLength)
    {
        // Arrange
        var request = ValidRequest();
        request.Currency = currency;
        request.CardNumber = new string('1', cardLength);
        request.Cvv = new string('0', cvvLength);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void When_RequestIsNull_Should_RejectRequest()
    {
        // Arrange & Act
        var result = _validator.Validate(null);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Payment request is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void When_CardNumberIsMissing_Should_ReturnOnlyRequiredError(string? cardNumber)
    {
        // Arrange
        var request = ValidRequest();
        request.CardNumber = cardNumber;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count().ShouldBe(1);
        result.Errors.ShouldContain("Card number is required.");
    }

    [Theory]
    [InlineData("1234567890123")]
    [InlineData("12345678901234567890")]
    [InlineData("222240534324001A")]
    [InlineData("１２３４５６７８９０１２３４")]
    public void When_CardNumberFormatIsInvalid_Should_RejectRequest(string cardNumber)
    {
        // Arrange
        var request = ValidRequest();
        request.CardNumber = cardNumber;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Card number must contain between 14 and 19 digits.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void When_ExpiryMonthIsOutOfRange_Should_RejectRequest(int month)
    {
        // Arrange
        var request = ValidRequest();
        request.ExpiryMonth = month;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Expiry month must be between 1 and 12.");
    }

    [Fact]
    public void When_CardHasExpired_Should_RejectRequest()
    {
        // Arrange
        var request = ValidRequest();
        request.ExpiryYear = DateTime.UtcNow.Year - 1;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Expiry month and year must be in the future.");
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("GB")]
    public void When_CurrencyIsUnsupported_Should_RejectRequest(string currency)
    {
        // Arrange
        var request = ValidRequest();
        request.Currency = currency;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Currency must be GBP, USD or EUR.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void When_AmountIsZeroOrNegative_Should_RejectRequest(int amount)
    {
        // Arrange
        var request = ValidRequest();
        request.Amount = amount;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Amount must be greater than zero.");
    }

    [Theory]
    [InlineData(null, "CVV is required.")]
    [InlineData("", "CVV is required.")]
    [InlineData("12", "CVV must contain 3 or 4 digits.")]
    [InlineData("12345", "CVV must contain 3 or 4 digits.")]
    [InlineData("12A", "CVV must contain 3 or 4 digits.")]
    public void When_CvvIsMissingOrInvalid_Should_RejectRequest(string? cvv, string expectedError)
    {
        // Arrange
        var request = ValidRequest();
        request.Cvv = cvv;

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(expectedError);
    }

    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumber = "2222405343240013",
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = 1050,
        Cvv = "123"
    };
}