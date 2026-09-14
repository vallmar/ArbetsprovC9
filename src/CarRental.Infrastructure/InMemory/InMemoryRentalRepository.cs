using System.Collections.Concurrent;
using CarRental.Application.Ports;
using CarRental.Domain;

namespace CarRental.Infrastructure.InMemory;

public sealed class InMemoryRentalRepository : IRentalRepository
{
    private readonly ConcurrentDictionary<string, Rental> rentals = new(StringComparer.OrdinalIgnoreCase);

    public Task<Rental?> GetByBookingNumberAsync(string bookingNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(rentals.GetValueOrDefault(bookingNumber));

    public Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        if (!rentals.TryAdd(rental.BookingNumber, rental))
            throw new InvalidOperationException("Booking number is already in use.");
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        rentals[rental.BookingNumber] = rental;
        return Task.CompletedTask;
    }
}
