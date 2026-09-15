using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarRental.Contracts;

namespace CustomerB.Postgres;

public sealed class RentalApiClient
{
    private static readonly JsonSerializerOptions CustomerJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient client;

    public RentalApiClient(string baseUrl, string tenantId)
    {
        client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantId);
    }

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
