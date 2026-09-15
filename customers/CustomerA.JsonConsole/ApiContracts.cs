using System.Net.Http.Json;
using CarRental.Contracts;

namespace CustomerA.JsonConsole;

public sealed class RentalApiClient(string baseUrl)
{
    private readonly HttpClient client = new() { BaseAddress = new Uri(baseUrl) };

    public async Task<RegisterPickupResponse> RegisterPickupAsync(
        CustomerRental rental,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/rentals/pickup",
            new RegisterPickupRequest(
                rental.BookingNumber,
                rental.RegistrationNumber,
                rental.CustomerId,
                rental.Category,
                rental.PickupTime,
                rental.PickupOdometer),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegisterPickupResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The SaaS API returned an empty pickup response.");
    }
}

public sealed record CustomerRental(
    string BookingNumber,
    string RegistrationNumber,
    string CustomerId,
    RentalCarCategory Category,
    DateTimeOffset PickupTime,
    int PickupOdometer);
