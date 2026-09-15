using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarRental.Contracts;

namespace CustomerA.JsonConsole;

public sealed class RentalApiClient(string baseUrl)
{
    private static readonly JsonSerializerOptions CustomerJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
            CustomerJsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CustomerJsonOptions, cancellationToken);
            throw new InvalidOperationException(error?.Error ?? "The rental service could not process the request.");
        }

        return await response.Content.ReadFromJsonAsync<RegisterPickupResponse>(CustomerJsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("The rental service returned an empty pickup response.");
    }
}

public sealed record CustomerRental(
    string BookingNumber,
    string RegistrationNumber,
    string CustomerId,
    ContractCarCategory Category,
    DateTimeOffset PickupTime,
    int PickupOdometer);
