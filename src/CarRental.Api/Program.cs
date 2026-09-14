using CarRental.Application.Ports;
using CarRental.Application.Pricing;
using CarRental.Application.Rentals;
using CarRental.Domain;
using CarRental.Infrastructure.InMemory;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IRentalRepository, InMemoryRentalRepository>();
builder.Services.AddSingleton<PriceCalculator>();
builder.Services.AddScoped<RentalService>();

var app = builder.Build();

app.MapPost("/api/rentals/pickup", async (RegisterPickupRequest request, RentalService service, CancellationToken ct) =>
{
    try
    {
        var rental = await service.RegisterPickupAsync(
            request.BookingNumber,
            request.RegistrationNumber,
            request.CustomerIdentifier,
            request.Category,
            request.PickupTime,
            request.PickupOdometer,
            ct);

        return Results.Created($"/api/rentals/{rental.BookingNumber}", rental);
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/rentals/{bookingNumber}/return", async (string bookingNumber, RegisterReturnRequest request, RentalService service, CancellationToken ct) =>
{
    try
    {
        var price = await service.RegisterReturnAsync(bookingNumber, request.ReturnTime, request.ReturnOdometer,
            new Pricing(request.BaseDailyPrice, request.BaseKmPrice), ct);
        return Results.Ok(new { bookingNumber, finalPrice = price });
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

public record RegisterPickupRequest(
    string BookingNumber,
    string RegistrationNumber,
    string CustomerIdentifier,
    CarCategory Category,
    DateTimeOffset PickupTime,
    int PickupOdometer);

public record RegisterReturnRequest(
    DateTimeOffset ReturnTime,
    int ReturnOdometer,
    decimal BaseDailyPrice,
    decimal BaseKmPrice);
