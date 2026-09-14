namespace CarRental.Domain;

public sealed record Pricing(decimal BaseDailyPrice, decimal BaseKmPrice)
{
    public Pricing
    {
        if (BaseDailyPrice < 0) throw new ArgumentOutOfRangeException(nameof(BaseDailyPrice));
        if (BaseKmPrice < 0) throw new ArgumentOutOfRangeException(nameof(BaseKmPrice));
    }
}
