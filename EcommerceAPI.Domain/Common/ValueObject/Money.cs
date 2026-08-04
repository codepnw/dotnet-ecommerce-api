namespace EcommerceAPI.Domain.Common.ValueObject;

public class Money : IEquatable<Money>
{
    public decimal Amount { get; init; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    // Factory Method for Create Object
    public static Money Create(decimal amount, string currency = "THB")
    {
        // amount 2 digit
        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        return new Money(roundedAmount, currency);
    }

    // Factory Method Default Value
    public static Money Zero(string currency = "THB") => new Money(0, currency);

    // Business logic currency
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    // Override Equals and GetHashCode for EF Core
    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Currency == other.Currency && Amount == other.Amount;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount} {Currency}";
}