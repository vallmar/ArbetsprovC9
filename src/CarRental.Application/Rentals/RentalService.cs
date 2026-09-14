using CarRental.Application.Ports;
using CarRental.Application.Pricing;
using CarRental.Domain;

namespace CarRental.Application.Rentals;

public sealed class RentalService(IRentalRepository repository, PriceCalculator priceCalculator)
{
    public async Task<Rental> RegisterPickupAsync(
        string bookingNumber,
        string registrationNumber,
        string customerIdentifier,
        CarCategory category,
        DateTimeOffset pickupTime,
        int pickupOdometer,
        CancellationToken cancellationToken = default)
    {
        if (await repository.GetByBookingNumberAsync(bookingNumber, cancellationToken) is not null)
            throw new InvalidOperationException("Booking number is already in use.");

        var rental = new Rental(bookingNumber, registrationNumber, customerIdentifier, category, pickupTime, pickupOdometer);
        await repository.AddAsync(rental, cancellationToken);
        return rental;
    }

    public async Task<decimal> RegisterReturnAsync(
        string bookingNumber,
        DateTimeOffset returnTime,
        int returnOdometer,
        Pricing pricing,
        CancellationToken cancellationToken = default)
    {
        var rental = await repository.GetByBookingNumberAsync(bookingNumber, cancellationToken)
            ?? throw new KeyNotFoundException($"Rental '{bookingNumber}' was not found.");

        if (rental.IsReturned)
            throw new InvalidOperationException("Rental has already been returned.");

        // Calculate using a temporary returned state, then persist the final price.
        rental.Return(returnTime, returnOdometer, 0m);
        var price = priceCalculator.Calculate(rental, pricing);
        rental.SetFinalPrice(price);

        await repository.UpdateAsync(rental, cancellationToken);
        return price;
    }
}
