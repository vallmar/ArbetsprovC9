namespace CarRental.Domain;

public sealed record Pricing
{
    public decimal BaseDailyPrice { get; }
    public decimal BaseKmPrice { get; }

    public Pricing(decimal baseDailyPrice, decimal baseKmPrice)
    {
        if (baseDailyPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(baseDailyPrice));

        if (baseKmPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(baseKmPrice));

        BaseDailyPrice = baseDailyPrice;
        BaseKmPrice = baseKmPrice;
    }
}
