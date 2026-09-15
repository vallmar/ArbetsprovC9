using CarRental.Domain;

namespace CarRental.Application.Pricing;

public sealed class PriceCalculator
{
    public decimal Calculate(Rental rental, CarRental.Domain.Pricing pricing)
    {
        ArgumentNullException.ThrowIfNull(rental);
        ArgumentNullException.ThrowIfNull(pricing);

        if (!rental.IsReturned || rental.ReturnTime is null || rental.ReturnOdometer is null)
            throw new InvalidOperationException("Cannot calculate the final rental price before return.");

        var days = Math.Max(1, (int)Math.Ceiling((rental.ReturnTime.Value - rental.PickupTime).TotalDays));
        var kilometers = rental.ReturnOdometer.Value - rental.PickupOdometer;

        return rental.Category switch
        {
            CarCategory.SmallCar => pricing.BaseDailyPrice * days,
            CarCategory.Combi => pricing.BaseDailyPrice * days * 1.3m + pricing.BaseKmPrice * kilometers,
            CarCategory.Truck => pricing.BaseDailyPrice * days * 1.5m + pricing.BaseKmPrice * kilometers * 1.5m,
            _ => throw new InvalidOperationException("The rental has an unsupported car category.")
        };
    }
}
