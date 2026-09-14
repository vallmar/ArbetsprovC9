namespace CarRental.Domain;

public sealed class Rental
{
    public string BookingNumber { get; }
    public string RegistrationNumber { get; }
    public string CustomerIdentifier { get; }
    public CarCategory Category { get; }
    public DateTimeOffset PickupTime { get; }
    public int PickupOdometer { get; }
    public DateTimeOffset? ReturnTime { get; private set; }
    public int? ReturnOdometer { get; private set; }
    public decimal? FinalPrice { get; private set; }

    public bool IsReturned => ReturnTime.HasValue;

    public Rental(string bookingNumber, string registrationNumber, string customerIdentifier,
        CarCategory category, DateTimeOffset pickupTime, int pickupOdometer)
    {
        if (string.IsNullOrWhiteSpace(bookingNumber)) throw new ArgumentException("Booking number is required.", nameof(bookingNumber));
        if (string.IsNullOrWhiteSpace(registrationNumber)) throw new ArgumentException("Registration number is required.", nameof(registrationNumber));
        if (string.IsNullOrWhiteSpace(customerIdentifier)) throw new ArgumentException("Customer identifier is required.", nameof(customerIdentifier));
        if (pickupOdometer < 0) throw new ArgumentOutOfRangeException(nameof(pickupOdometer));

        BookingNumber = bookingNumber;
        RegistrationNumber = registrationNumber;
        CustomerIdentifier = customerIdentifier;
        Category = category;
        PickupTime = pickupTime;
        PickupOdometer = pickupOdometer;
    }

    public void Return(DateTimeOffset returnTime, int returnOdometer, decimal finalPrice)
    {
        if (IsReturned) throw new InvalidOperationException("Rental has already been returned.");
        if (returnTime < PickupTime) throw new ArgumentException("Return time cannot be before pickup time.", nameof(returnTime));
        if (returnOdometer < PickupOdometer) throw new ArgumentOutOfRangeException(nameof(returnOdometer));
        if (finalPrice < 0) throw new ArgumentOutOfRangeException(nameof(finalPrice));

        ReturnTime = returnTime;
        ReturnOdometer = returnOdometer;
        FinalPrice = finalPrice;
    }
}
