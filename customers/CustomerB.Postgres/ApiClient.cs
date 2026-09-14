using System.Net.Http.Json;

namespace CustomerB.Postgres;

public sealed class RentalApiClient(string baseUrl)
{
    private readonly HttpClient client = new() { BaseAddress = new Uri(baseUrl) };

    public async Task RegisterPickupAsync(CustomerRental rental, CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync("/api/rentals/pickup", new
        {
            rental.BookingNumber,
            rental.RegistrationNumber,
            CustomerIdentifier = rental.CustomerId,
            rental.Category,
            rental.PickupTime,
            rental.PickupOdometer
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}

public sealed record CustomerRental(
    string BookingNumber,
    string RegistrationNumber,
    string CustomerId,
    string Category,
    DateTimeOffset PickupTime,
    int PickupOdometer);
