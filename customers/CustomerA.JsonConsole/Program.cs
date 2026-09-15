using System.Text.Json;
using CustomerA.JsonConsole;
using CarRental.Contracts;

var store = new CustomerAJsonStore("customer-a-data.json");
var api = new RentalApiClient("http://localhost:5000");

Console.WriteLine("Customer A - JSON storage + Console UI");
Console.WriteLine("The customer owns its storage model and integrates with the SaaS through public API contracts.");

var pickup = new CustomerRental(
    "A-1001",
    "ABC123",
    "customer-a-1",
    RentalCarCategory.SmallCar,
    DateTimeOffset.UtcNow,
    10000);

await store.SaveAsync(pickup);
Console.WriteLine($"Saved {pickup.BookingNumber} in customer-owned JSON storage.");

// Live integration against the SaaS API:
// var response = await api.RegisterPickupAsync(pickup);
// Console.WriteLine($"SaaS accepted pickup {response.BookingNumber}.");

sealed class CustomerAJsonStore(string path)
{
    public async Task SaveAsync(CustomerRental rental)
    {
        var json = JsonSerializer.Serialize(rental, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
}
