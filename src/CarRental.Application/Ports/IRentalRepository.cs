using CarRental.Domain;

namespace CarRental.Application.Ports;

public interface IRentalRepository
{
    Task<Rental?> GetByBookingNumberAsync(string bookingNumber, CancellationToken cancellationToken = default);
    Task AddAsync(Rental rental, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default);
}
