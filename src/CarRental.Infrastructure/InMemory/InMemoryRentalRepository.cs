using System.Collections.Concurrent;
using CarRental.Application.Ports;
using CarRental.Domain;

namespace CarRental.Infrastructure.InMemory;

public sealed class InMemoryRentalRepository : IRentalRepository
{
    private readonly ConcurrentDictionary<(string TenantId, string BookingNumber), Rental> rentals = new();

    public Task<Rental?> GetByBookingNumberAsync(string tenantId, string bookingNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(rentals.GetValueOrDefault((tenantId, bookingNumber)));

    public Task<string?> GetOwnerTenantIdByBookingNumberAsync(string bookingNumber, CancellationToken cancellationToken = default)
    {
        var match = rentals.Keys.FirstOrDefault(key => key.BookingNumber == bookingNumber);
        return Task.FromResult(match == default ? null : match.TenantId);
    }

    public Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        if (!rentals.TryAdd((rental.TenantId, rental.BookingNumber), rental))
            throw new InvalidOperationException("Booking number is already in use.");
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        rentals[(rental.TenantId, rental.BookingNumber)] = rental;
        return Task.CompletedTask;
    }
}
