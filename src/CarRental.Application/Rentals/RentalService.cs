using CarRental.Application.Ports;
using CarRental.Application.Pricing;
using CarRental.Domain;
using Microsoft.Extensions.Logging;

namespace CarRental.Application.Rentals;

public sealed class RentalService(
    IRentalRepository repository,
    PriceCalculator priceCalculator,
    ITenantContext tenantContext,
    ILogger<RentalService> logger)
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
        if (await repository.GetByBookingNumberAsync(tenantContext.TenantId, bookingNumber, cancellationToken) is not null)
            throw new InvalidOperationException("Booking number is already in use.");

        var rental = new Rental(tenantContext.TenantId, bookingNumber, registrationNumber, customerIdentifier, category, pickupTime, pickupOdometer);
        await repository.AddAsync(rental, cancellationToken);
        return rental;
    }

    public async Task<decimal> RegisterReturnAsync(
        string bookingNumber,
        DateTimeOffset returnTime,
        int returnOdometer,
        CarRental.Domain.Pricing pricing,
        CancellationToken cancellationToken = default)
    {
        var rental = await repository.GetByBookingNumberAsync(tenantContext.TenantId, bookingNumber, cancellationToken);
        if (rental is null)
        {
            var ownerTenantId = await repository.GetOwnerTenantIdByBookingNumberAsync(bookingNumber, cancellationToken);
            if (ownerTenantId is not null && !string.Equals(ownerTenantId, tenantContext.TenantId, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "Cross-tenant rental access attempt blocked. Tenant {TenantId} attempted to access booking {BookingNumber} owned by tenant {OwnerTenantId}.",
                    tenantContext.TenantId,
                    bookingNumber,
                    ownerTenantId);
            }

            throw new KeyNotFoundException($"Rental '{bookingNumber}' was not found.");
        }

        rental.Return(returnTime, returnOdometer);
        var price = priceCalculator.Calculate(rental, pricing);
        rental.SetFinalPrice(price);
        await repository.UpdateAsync(rental, cancellationToken);
        return price;
    }
}
