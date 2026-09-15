using CarRental.Application.Ports;
using CarRental.Application.Pricing;
using CarRental.Application.Rentals;
using CarRental.Contracts;
using CarRental.Domain;
using CarRental.Infrastructure.InMemory;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<IRentalRepository, InMemoryRentalRepository>();
builder.Services.AddSingleton<PriceCalculator>();
builder.Services.AddScoped<RentalService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        if (exception is not null)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {HttpMethod} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Json(new ErrorResponse("An unexpected error occurred.")).ExecuteAsync(context);
    });
});

app.MapPost("/api/rentals/pickup", async (RegisterPickupRequest request, RentalService service, CancellationToken ct) =>
{
    try
    {
        var rental = await service.RegisterPickupAsync(
            request.BookingNumber,
            request.RegistrationNumber,
            request.CustomerIdentifier,
            ToDomainCategory(request.Category),
            request.PickupTime,
            request.PickupOdometer,
            ct);

        var response = new RegisterPickupResponse(
            rental.BookingNumber,
            rental.RegistrationNumber,
            rental.CustomerIdentifier,
            ToContractCategory(rental.Category),
            rental.PickupTime,
            rental.PickupOdometer,
            rental.IsReturned);

        return Results.Created($"/api/rentals/{rental.BookingNumber}", response);
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
});

app.MapPost("/api/rentals/{bookingNumber}/return", async (string bookingNumber, RegisterReturnRequest request, RentalService service, CancellationToken ct) =>
{
    try
    {
        var price = await service.RegisterReturnAsync(
            bookingNumber,
            request.ReturnTime,
            request.ReturnOdometer,
            new Pricing(request.BaseDailyPrice, request.BaseKmPrice),
            ct);

        return Results.Ok(new RegisterReturnResponse(bookingNumber, price));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new ErrorResponse(ex.Message));
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
});

#if DEBUG
app.MapGet("/api/test/unhandled-error", () => throw new InvalidOperationException("Intentional test exception."));
#endif

app.Run();

public partial class Program
{
    static CarCategory ToDomainCategory(ContractCarCategory category) => category switch
    {
        ContractCarCategory.SmallCar => CarCategory.SmallCar,
        ContractCarCategory.Combi => CarCategory.Combi,
        ContractCarCategory.Truck => CarCategory.Truck,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown car category.")
    };

    static ContractCarCategory ToContractCategory(CarCategory category) => category switch
    {
        CarCategory.SmallCar => ContractCarCategory.SmallCar,
        CarCategory.Combi => ContractCarCategory.Combi,
        CarCategory.Truck => ContractCarCategory.Truck,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown car category.")
    };
}