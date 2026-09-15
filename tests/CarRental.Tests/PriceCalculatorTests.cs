using CarRental.Application.Pricing;
using CarRental.Domain;
using Xunit;

namespace CarRental.Tests;

public class PriceCalculatorTests
{
    private readonly PriceCalculator calculator = new();
    private readonly Pricing pricing = new(500m, 2m);

    [Fact]
    public void Small_car_uses_only_daily_rate()
    {
        var rental = ReturnedRental(CarCategory.SmallCar, 3, 100);
        Assert.Equal(1500m, calculator.Calculate(rental, pricing));
    }

    [Fact]
    public void Combi_applies_daily_multiplier_and_kilometers()
    {
        var rental = ReturnedRental(CarCategory.Combi, 3, 100);
        Assert.Equal(2150m, calculator.Calculate(rental, pricing));
    }

    [Fact]
    public void Truck_applies_multiplier_to_both_components()
    {
        var rental = ReturnedRental(CarCategory.Truck, 3, 100);
        Assert.Equal(2550m, calculator.Calculate(rental, pricing));
    }

    [Fact]
    public void Different_base_prices_change_result()
    {
        var rental = ReturnedRental(CarCategory.Combi, 2, 50);
        var differentPricing = new Pricing(1000m, 5m);
        Assert.Equal(2850m, calculator.Calculate(rental, differentPricing));
    }

    private static Rental ReturnedRental(CarCategory category, int days, int kilometers)
    {
        var rental = new Rental("B-1", "ABC123", "customer-1", category,
            DateTimeOffset.Parse("2026-01-01T10:00:00+01:00"), 10_000);
        rental.Return(rental.PickupTime.AddDays(days), rental.PickupOdometer + kilometers);
        return rental;
    }
}
