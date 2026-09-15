namespace CarRental.Domain;

public sealed record Pricing(decimal BaseDailyPrice, decimal BaseKmPrice)
{
    public Pricing(decimal baseDailyPrice, decimal baseKmPrice)
        : this()
    {
        if (baseDailyPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(baseDailyPrice));
        if (baseKmPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(baseKmPrice));

        BaseDailyPrice = baseDailyPrice;
        BaseKmPrice = baseKmPrice;
    }
}
