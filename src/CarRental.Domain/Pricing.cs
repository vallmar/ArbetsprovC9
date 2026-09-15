namespace CarRental.Domain;

public sealed record Pricing
{
    public decimal BaseDailyPrice { get; }
    public decimal BaseKmPrice { get; }

    public Pricing(decimal baseDailyPrice, decimal baseKmPrice)
    {
        if (baseDailyPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(baseDailyPrice), "Base daily price cannot be negative.");

        if (baseKmPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(baseKmPrice), "Base kilometer price cannot be negative.");

        BaseDailyPrice = baseDailyPrice;
        BaseKmPrice = baseKmPrice;
    }
}