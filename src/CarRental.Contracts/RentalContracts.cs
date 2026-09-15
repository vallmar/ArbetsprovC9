namespace CarRental.Contracts;

public enum RentalCarCategory
{
    SmallCar,
    Combi,
    Truck
}

public sealed record RegisterPickupRequest(
    string BookingNumber,
    string RegistrationNumber,
    string CustomerIdentifier,
    RentalCarCategory Category,
    DateTimeOffset PickupTime,
    int PickupOdometer);

public sealed record RegisterPickupResponse(
    string BookingNumber,
    string RegistrationNumber,
    string CustomerIdentifier,
    RentalCarCategory Category,
    DateTimeOffset PickupTime,
    int PickupOdometer,
    bool IsReturned);

public sealed record RegisterReturnRequest(
    DateTimeOffset ReturnTime,
    int ReturnOdometer,
    decimal BaseDailyPrice,
    decimal BaseKmPrice);

public sealed record RegisterReturnResponse(
    string BookingNumber,
    decimal FinalPrice);

public sealed record ErrorResponse(string Error);
