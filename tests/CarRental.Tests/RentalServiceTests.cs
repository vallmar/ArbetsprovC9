using CarRental.Application.Ports;
using CarRental.Application.Pricing;
using CarRental.Application.Rentals;
using CarRental.Domain;
using Xunit;

namespace CarRental.Tests;

public sealed class RentalServiceTests
{
    [Fact]
    public async Task Register_return_calculates_and_persists_final_price()
    {
        var repository = new InMemoryTestRepository();
        var service = new RentalService(repository, new PriceCalculator());

        await service.RegisterPickupAsync("B-1", "ABC123", "customer-1", CarCategory.Combi, DateTimeOffset.Parse("2026-01-01T10:00:00+01:00"), 10_000, TestContext.Current.CancellationToken);

        var price = await service.RegisterReturnAsync("B-1", DateTimeOffset.Parse("2026-01-03T10:00:00+01:00"), 10_100, new Pricing(500m, 2m), TestContext.Current.CancellationToken);

        Assert.Equal(1500m, price);
        Assert.Equal(1500m, repository.Items["B-1"].FinalPrice);
    }

    [Fact]
    public async Task Unknown_booking_number_fails_on_return()
    {
        var service = new RentalService(new InMemoryTestRepository(), new PriceCalculator());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RegisterReturnAsync("missing", DateTimeOffset.Parse("2026-01-03T10:00:00+01:00"), 10_100, new Pricing(500m, 2m), TestContext.Current.CancellationToken));
    }

    private sealed class InMemoryTestRepository : IRentalRepository
    {
        public Dictionary<string, Rental> Items { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Task<Rental?> GetByBookingNumberAsync(string bookingNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.GetValueOrDefault(bookingNumber));
        public Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
        {
            if (!Items.TryAdd(rental.BookingNumber, rental)) throw new InvalidOperationException();
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
        {
            Items[rental.BookingNumber] = rental;
            return Task.CompletedTask;
        }
    }
}
