# Customer API documentation

This document describes the HTTP API exposed by the car-rental SaaS.

The API is the integration boundary for customers. Customer applications should treat the JSON request and response models documented here as the public contract and should not depend on the internal Domain or Application models.

## Base URL

The examples use:

```text
http://localhost:5000
```

In a deployed environment this will be replaced by the customer's SaaS API URL.

## Response handling

A customer integration must always handle the HTTP status code first and then inspect the JSON response body.

Successful responses use endpoint-specific response contracts.

Error responses from business logic use this common shape:

```json
{
  "error": "Booking number is already in use."
}
```

The `error` property contains a human-readable explanation of what prevented the requested operation. Customer applications should display or log this message for troubleshooting, but should use the HTTP status code to decide how the request should be handled programmatically.

Do not assume that every `400` response has the same cause. A `400` can mean that the request is invalid according to the API/business rules. The `error` text explains the specific reason.

For malformed JSON or JSON values that cannot be deserialized into the request contract, ASP.NET may reject the request before the endpoint handler runs. Such framework-level validation errors may use a different response shape. Customer integrations should therefore not assume that every error response contains the `error` property.

## POST /api/rentals/pickup

Registers a vehicle pickup.

### Request

```http
POST /api/rentals/pickup
Content-Type: application/json
```

```json
{
  "bookingNumber": "TEST-001",
  "registrationNumber": "ABC123",
  "customerIdentifier": "customer-a",
  "category": "SmallCar",
  "pickupTime": "2026-09-15T10:00:00Z",
  "pickupOdometer": 10000
}
```

`category` is serialized as a string. Supported values are:

- `SmallCar`
- `Combi`
- `Truck`

### Success

HTTP `201 Created`.

Example response:

```json
{
  "bookingNumber": "TEST-001",
  "registrationNumber": "ABC123",
  "customerIdentifier": "customer-a",
  "category": "SmallCar",
  "pickupTime": "2026-09-15T10:00:00Z",
  "pickupOdometer": 10000,
  "isReturned": false
}
```

The `Location` header points to:

```text
/api/rentals/{bookingNumber}
```

### Error responses

#### HTTP 400 Bad Request

The request reached the application but could not be accepted.

Example:

```json
{
  "error": "Booking number is already in use."
}
```

Possible business/domain messages for pickup include:

| Error message | Meaning | Customer action |
|---|---|---|
| `Booking number is already in use.` | A rental with the supplied booking number already exists. | Do not retry with the same booking number. Verify the booking number or retrieve the existing rental through the customer's own booking flow. |
| `Booking number is required.` | `bookingNumber` is empty or whitespace. | Supply a non-empty booking number. |
| `Registration number is required.` | `registrationNumber` is empty or whitespace. | Supply a non-empty registration number. |
| `Customer identifier is required.` | `customerIdentifier` is empty or whitespace. | Supply a non-empty customer identifier. |
| `Unknown car category.` | The category value is not one of the supported categories. | Use `SmallCar`, `Combi`, or `Truck`. |

An invalid negative `pickupOdometer` is also rejected. The current implementation exposes the standard argument exception message for this case, so clients should treat the message as diagnostic text rather than as a stable machine-readable error code.

### Example customer handling

```text
POST pickup
    |
    +-- 201 --> Parse RegisterPickupResponse and continue
    |
    +-- 400 --> Parse error when present, show/log it, correct the request
    |
    +-- other 4xx/5xx --> Handle as HTTP/API failure and inspect the body
```

## POST /api/rentals/{bookingNumber}/return

Registers the return of a rental and calculates the final price.

### Request

```http
POST /api/rentals/TEST-001/return
Content-Type: application/json
```

```json
{
  "returnTime": "2026-09-15T18:00:00Z",
  "returnOdometer": 10150,
  "baseDailyPrice": 500,
  "baseKmPrice": 2
}
```

### Success

HTTP `200 OK`.

Example response:

```json
{
  "bookingNumber": "TEST-001",
  "finalPrice": 1000
}
```

`finalPrice` is the calculated final rental price.

### Error responses

#### HTTP 404 Not Found

The booking number does not exist in the SaaS rental store.

Example:

```json
{
  "error": "Rental 'TEST-001' was not found."
}
```

**Meaning:** the customer asked to return a rental that the SaaS does not know about.

**Customer action:** verify the booking number. Do not retry unchanged indefinitely.

#### HTTP 400 Bad Request

The rental exists, but the return request violates a business/domain rule.

Possible messages include:

| Error message | Meaning | Customer action |
|---|---|---|
| `Rental has already been returned.` | The rental is already in the returned state. | Treat the operation as already completed; do not submit another return for the same booking. |
| `Return time cannot be before pickup time.` | The supplied return timestamp is earlier than the pickup timestamp. | Correct `returnTime`. |
| `A rental must be returned before a final price can be set.` | A final price cannot be assigned to an active rental. | Normally this indicates an internal sequencing problem; the return must be registered before pricing. |

A negative `returnOdometer` or a return odometer below the pickup odometer is rejected. The current implementation exposes standard argument exception text for those cases, so the message should be treated as diagnostic text rather than a stable error code.

Invalid or negative pricing values are also rejected because the pricing value object requires non-negative base prices.

## Error message policy

The current API deliberately returns a simple customer-facing message in the `ErrorResponse.Error` property for business/application errors.

Example:

```json
{
  "error": "Rental has already been returned."
}
```

This makes the response easy for a customer's UI or integration layer to display:

```text
Request failed (400)
Rental has already been returned.
```

However, the `error` string is not currently a versioned error code. Customers should therefore use the HTTP status code and request context for programmatic handling and use the text primarily for display, logging, and troubleshooting.

## Recommended client implementation

A customer API client should follow this pattern:

1. Send the HTTP request.
2. Check the HTTP status code.
3. On success, deserialize the endpoint's documented response type.
4. On error, attempt to deserialize `{ "error": "..." }`.
5. Display/log the `error` text when it is available.
6. For responses without the `error` property, keep the HTTP status and raw response body available for diagnostics because the API framework may have generated the response before the application endpoint ran.

Conceptually:

```csharp
if (response.IsSuccessStatusCode)
{
    // Deserialize the endpoint-specific success response.
}
else
{
    // Prefer the API's ErrorResponse.error when present.
    // Keep status code and raw body for diagnostics.
}
```

## Contract summary

| Endpoint | Success | Business/application error | Typical meaning |
|---|---:|---:|---|
| `POST /api/rentals/pickup` | `201 Created` | `400 Bad Request` | Pickup accepted / request violates a rule |
| `POST /api/rentals/{bookingNumber}/return` | `200 OK` | `400 Bad Request`, `404 Not Found` | Return accepted / request violates a rule / rental not found |

The public JSON contracts are defined in `src/CarRental.Contracts/RentalContracts.cs`. The API maps these contracts to the internal domain model and does not expose the internal `Rental` type directly.