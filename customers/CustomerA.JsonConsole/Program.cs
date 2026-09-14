using System.Text.Json;

var store = new CustomerAJsonStore("customer-a-data.json");
var api = new RentalApiClient("http://localhost:5000");

Console.WriteLine("Customer A - JSON storage + Console UI");
Console.WriteLine("This application owns its storage model and only knows the SaaS HTTP API contract.");

var booking = "A-1001";
var pickup = new CustomerRental("A-1001", "ABC123", "customer-a-1", "SmallCar",
    DateTimeOffset.UtcNow, 10000);

await store.SaveAsync(pickup);
Console.WriteLine($"Saved {pickup.BookingNumber} in customer-owned JSON storage.");

// Optional live integration against the SaaS API:
// await api.RegisterPickupAsync(pickup);

return;

sealed record CustomerRental(string BookingNumber, string RegistrationNumber, string CustomerId,
    string Category, DateTimeOffset PickupTime, int PickupOdometer);

sealed class CustomerAJsonStore(string path)
{
    public async Task SaveAsync(CustomerRental rental)
    {
        var json = JsonSerializer.Serialize(rental, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
}

sealed class RentalApiClient(string baseUrl)
{
    private readonly HttpClient client = new() { BaseAddress = new Uri(baseUrl) };

    public async Task RegisterPickupAsync(CustomerRental rental)
    {
        var response = await client.PostAsJsonAsync("/api/rentals/pickup", new
        {
            rental.BookingNumber,
            rental.RegistrationNumber,
            CustomerIdentifier = rental.CustomerId,
            Category = rental.Category,
            rental.PickupTime,
            rental.PickupOdometer
        });
        response.EnsureSuccessStatusCode();
    }
}
