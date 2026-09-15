using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarRental.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CarRental.Tests;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions CustomerJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Pickup_returns_201_and_customer_response_contract()
    {
        var bookingNumber = NewBookingNumber();
        var request = new RegisterPickupRequest(
            bookingNumber,
            "ABC123",
            "customer-a",
            ContractCarCategory.SmallCar,
            DateTimeOffset.Parse("2026-09-15T10:00:00Z"),
            10000);

        var response = await PostAsCustomerJsonAsync("/api/rentals/pickup", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadCustomerJsonAsync<RegisterPickupResponse>(response);
        Assert.NotNull(body);
        Assert.Equal(bookingNumber, body!.BookingNumber);
        Assert.Equal("ABC123", body.RegistrationNumber);
        Assert.Equal("customer-a", body.CustomerIdentifier);
        Assert.Equal(ContractCarCategory.SmallCar, body.Category);
        Assert.Equal(10000, body.PickupOdometer);
        Assert.False(body.IsReturned);
        Assert.Equal($"/api/rentals/{bookingNumber}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Pickup_returns_400_with_customer_error_message_when_booking_number_is_duplicate()
    {
        var bookingNumber = NewBookingNumber();
        var request = new RegisterPickupRequest(
            bookingNumber,
            "ABC123",
            "customer-a",
            ContractCarCategory.SmallCar,
            DateTimeOffset.Parse("2026-09-15T10:00:00Z"),
            10000);

        var firstResponse = await PostAsCustomerJsonAsync("/api/rentals/pickup", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await PostAsCustomerJsonAsync("/api/rentals/pickup", request);

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var error = await ReadCustomerJsonAsync<ErrorResponse>(secondResponse);
        Assert.NotNull(error);
        Assert.Equal("Booking number is already in use.", error!.Error);
    }

    [Fact]
    public async Task Pickup_accepts_string_car_category_from_customer_json()
    {
        var bookingNumber = NewBookingNumber();
        var json = JsonSerializer.Serialize(new
        {
            bookingNumber,
            registrationNumber = "ABC123",
            customerIdentifier = "customer-a",
            category = "SmallCar",
            pickupTime = "2026-09-15T10:00:00Z",
            pickupOdometer = 10000
        }, CustomerJsonOptions);

        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/rentals/pickup", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadCustomerJsonAsync<RegisterPickupResponse>(response);
        Assert.NotNull(body);
        Assert.Equal(ContractCarCategory.SmallCar, body!.Category);
    }

    [Fact]
    public async Task Return_returns_200_and_final_price()
    {
        var bookingNumber = NewBookingNumber();
        await RegisterPickupAsync(bookingNumber, ContractCarCategory.Combi, 10000);

        var request = new RegisterReturnRequest(
            DateTimeOffset.Parse("2026-09-15T18:00:00Z"),
            10100,
            500m,
            2m);

        var response = await PostAsCustomerJsonAsync($"/api/rentals/{bookingNumber}/return", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadCustomerJsonAsync<RegisterReturnResponse>(response);
        Assert.NotNull(body);
        Assert.Equal(bookingNumber, body!.BookingNumber);
        Assert.Equal(850m, body.FinalPrice);
    }

    [Fact]
    public async Task Return_returns_404_with_error_message_when_rental_does_not_exist()
    {
        var bookingNumber = NewBookingNumber();
        var request = new RegisterReturnRequest(
            DateTimeOffset.Parse("2026-09-15T18:00:00Z"),
            10100,
            500m,
            2m);

        var response = await PostAsCustomerJsonAsync($"/api/rentals/{bookingNumber}/return", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await ReadCustomerJsonAsync<ErrorResponse>(response);
        Assert.NotNull(error);
        Assert.Equal($"Rental '{bookingNumber}' was not found.", error!.Error);
    }

    [Fact]
    public async Task Return_returns_400_with_error_message_when_rental_has_already_been_returned()
    {
        var bookingNumber = NewBookingNumber();
        await RegisterPickupAsync(bookingNumber, ContractCarCategory.SmallCar, 10000);

        var request = new RegisterReturnRequest(
            DateTimeOffset.Parse("2026-09-15T18:00:00Z"),
            10100,
            500m,
            2m);

        var firstResponse = await PostAsCustomerJsonAsync($"/api/rentals/{bookingNumber}/return", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await PostAsCustomerJsonAsync($"/api/rentals/{bookingNumber}/return", request);

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var error = await ReadCustomerJsonAsync<ErrorResponse>(secondResponse);
        Assert.NotNull(error);
        Assert.Equal("Rental has already been returned.", error!.Error);
    }

    [Fact]
    public async Task Return_returns_400_with_error_message_when_return_time_is_before_pickup()
    {
        var bookingNumber = NewBookingNumber();
        await RegisterPickupAsync(bookingNumber, ContractCarCategory.SmallCar, 10000);

        var request = new RegisterReturnRequest(
            DateTimeOffset.Parse("2026-09-15T09:00:00Z"),
            10100,
            500m,
            2m);

        var response = await PostAsCustomerJsonAsync($"/api/rentals/{bookingNumber}/return", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadCustomerJsonAsync<ErrorResponse>(response);
        Assert.NotNull(error);
        Assert.Equal("Return time cannot be before pickup time.", error!.Error);
    }

    [Fact]
    public async Task Return_returns_400_with_customer_message_when_return_odometer_is_lower_than_pickup()
    {
        var bookingNumber = NewBookingNumber();
        await RegisterPickupAsync(bookingNumber, ContractCarCategory.SmallCar, 10000);

        var request = new RegisterReturnRequest(
            DateTimeOffset.Parse("2026-09-15T18:00:00Z"),
            9999,
            500m,
            2m);

        var response = await PostAsCustomerJsonAsync($"/api/rentals/{bookingNumber}/return", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadCustomerJsonAsync<ErrorResponse>(response);
        Assert.NotNull(error);
        Assert.Equal("Return odometer cannot be lower than pickup odometer.", error!.Error);
    }

    private async Task RegisterPickupAsync(string bookingNumber, ContractCarCategory category, int odometer)
    {
        var request = new RegisterPickupRequest(
            bookingNumber,
            "ABC123",
            "customer-a",
            category,
            DateTimeOffset.Parse("2026-09-15T10:00:00Z"),
            odometer);

        var response = await PostAsCustomerJsonAsync("/api/rentals/pickup", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostAsCustomerJsonAsync<T>(string uri, T value)
        => client.PostAsJsonAsync(uri, value, CustomerJsonOptions);

    private static Task<T?> ReadCustomerJsonAsync<T>(HttpResponseMessage response)
        => response.Content.ReadFromJsonAsync<T>(CustomerJsonOptions);

    private static string NewBookingNumber() => $"TEST-{Guid.NewGuid():N}";
}
