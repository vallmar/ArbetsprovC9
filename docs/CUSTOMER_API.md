# Customer API documentation

This document describes the HTTP API exposed by the car-rental SaaS.

The API is the integration boundary for customers. Customer applications should treat the JSON request and response models documented here as the public contract and should not depend on the internal Domain or Application models.

## Base URL

The examples use:

```text
http://localhost:5000
```

In a deployed environment this will be replaced by the customer's SaaS API URL.

## Response and error handling

The most important rule for a customer integration is:

> **Check the HTTP status code first. Then inspect the response body.**

A successful request returns the endpoint-specific response documented below.

Business/application errors use this JSON shape:

```json
{
  "error": "Booking number is already in use."
}
```

The `error` value is intended to be understandable to a human. A customer application can display it directly in a UI and should also log it when troubleshooting.

Example UI handling:

```text
Request failed (400)
Booking number is already in use.
```

### Important: do not rely on the error text for program logic

The HTTP status code is the stable signal for deciding how the request failed. The `error` string explains the specific reason.

For example, a customer should treat `400` as a rejected request and use the message to tell the user what needs correcting. The customer should **not** write business logic such as `if error == "Booking number is already in use."`.

The current API does not expose a separate machine-readable error code.

### Errors generated before the endpoint runs

Malformed JSON or JSON values that cannot be converted to the request contract can be rejected by ASP.NET before the endpoint handler is entered. These errors may therefore have a different JSON shape and may not contain the `error` property.

Customer clients must consequently handle both:

```json
{
  "error": "..."
}
```

and framework-generated error responses.

For an error without an `error` property, keep the HTTP status code and response body available for diagnostics instead of assuming a specific business error.

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

### Request fields

| Field | Type | Required | Description |
|---|---|---|---|
| `bookingNumber` | string | Yes | Customer's booking identifier. Must not be empty. Must be unique in the SaaS rental store. |
| `registrationNumber` | string | Yes | Vehicle registration number. Must not be empty. |
| `customerIdentifier` | string | Yes | Identifier for the customer making the rental. Must not be empty. |
| `category` | string | Yes | Vehicle category: `SmallCar`, `Combi`, or `Truck`. |
| `pickupTime` | ISO-8601 timestamp | Yes | Time at which the vehicle was picked up. |
| `pickupOdometer` | integer | Yes | Odometer reading at pickup. Must not be negative. |

### Success response

HTTP `201 Created`.

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

The response confirms that the pickup has been registered. `isReturned` is `false` immediately after pickup.

The response also contains a `Location` header pointing to:

```text
/api/rentals/{bookingNumber}
```

### Error responses

#### `400 Bad Request`

The request was understood by the API but could not be accepted because it violates an input or business rule.

Typical response:

```json
{
  "error": "Booking number is already in use."
}
```

#### Error message reference

| Error message | What it means | What the customer should do |
|---|---|---|
| `Booking number is already in use.` | A rental with this booking number already exists. | Verify the booking number. Do not retry unchanged. |
| `Booking number is required.` | `bookingNumber` is empty or whitespace. | Supply a non-empty booking number. |
| `Registration number is required.` | `registrationNumber` is empty or whitespace. | Supply a non-empty registration number. |
| `Customer identifier is required.` | `customerIdentifier` is empty or whitespace. | Supply a non-empty customer identifier. |
| `Unknown car category.` | The category could not be mapped to one of the supported categories. | Use `SmallCar`, `Combi`, or `Truck`. |
| `...` for a negative pickup odometer | The pickup odometer is invalid because it is below zero. The current implementation exposes the standard .NET argument-exception message rather than a dedicated application message. | Correct `pickupOdometer` and resend the request. |

### Example customer handling

```text
POST /api/rentals/pickup
        |
        +-- 201 --> Deserialize RegisterPickupResponse
        |           Continue normal customer workflow
        |
        +-- 400 --> Read error when present
        |           Show/log the message
        |           Correct the request
        |
        +-- other 4xx/5xx --> Treat as API/framework failure
                              Keep status + response body for diagnostics
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

### Request fields

| Field | Type | Required | Description |
|---|---|---|---|
| `bookingNumber` | path string | Yes | Booking to return. |
| `returnTime` | ISO-8601 timestamp | Yes | Time at which the vehicle was returned. Cannot be before pickup time. |
| `returnOdometer` | integer | Yes | Odometer reading at return. Cannot be below the pickup odometer. |
| `baseDailyPrice` | decimal | Yes | Base daily rental price. Must not be negative. |
| `baseKmPrice` | decimal | Yes | Base kilometre price. Must not be negative. |

### Success response

HTTP `200 OK`.

```json
{
  "bookingNumber": "TEST-001",
  "finalPrice": 1000
}
```

`finalPrice` is the calculated final rental price.

### Error responses

#### `404 Not Found`

The specified booking does not exist.

```json
{
  "error": "Rental 'TEST-001' was not found."
}
```

**Meaning:** the SaaS cannot find a rental for that booking number.

**Customer action:** verify that the booking number is correct and that the pickup was successfully registered.

#### `400 Bad Request`

The rental exists, but the return request violates a business or domain rule.

| Error message | What it means | What the customer should do |
|---|---|---|
| `Rental has already been returned.` | The rental is already in the returned state. | Treat the return as already completed. Do not submit another return. |
| `Return time cannot be before pickup time.` | `returnTime` is earlier than the original pickup time. | Correct `returnTime`. |
| `...` for an invalid return odometer | The return odometer is below the pickup odometer or otherwise invalid. The current implementation exposes the standard .NET argument-exception message. | Correct `returnOdometer`. |
| `...` for invalid pricing | One of the base prices is negative. The pricing value object rejects negative values. | Supply non-negative `baseDailyPrice` and `baseKmPrice`. |

### Error handling example

If the API returns:

```json
{
  "error": "Rental has already been returned."
}
```

the customer UI can display:

```text
Return failed (400)
Rental has already been returned.
```

The application should not interpret the English sentence as an error code. It should interpret the `400` status as a rejected request and use the message to explain why.

## How customers should display errors

The API's business errors are intentionally simple so that a customer can surface them directly.

Recommended flow:

```text
HTTP response
    |
    +-- 2xx --> deserialize documented success response
    |
    +-- error --> check status code
                  |
                  +-- body contains "error"
                  |       -> display/log that message
                  |
                  +-- no "error"
                          -> display a generic API error
                          -> retain status + raw response for diagnostics
```

A good customer-facing implementation might therefore show:

```text
Could not register return.
Rental 'ABC-123' was not found.
```

while logging the HTTP status and raw response for support/debugging.

## Current error contract and versioning

The current public error contract is:

```json
{
  "error": "Human-readable message"
}
```

The `error` text is useful for humans but is **not a stable machine-readable identifier**. This means customers should not build conditional logic around exact message text.

A future version of the API could add a stable code without removing the message, for example:

```json
{
  "code": "BOOKING_NUMBER_IN_USE",
  "error": "Booking number is already in use."
}
```

The current implementation does not yet provide such a `code` field.

## Contract summary

| Endpoint | Success | Business/application errors |
|---|---:|---|
| `POST /api/rentals/pickup` | `201 Created` | `400 Bad Request` |
| `POST /api/rentals/{bookingNumber}/return` | `200 OK` | `400 Bad Request`, `404 Not Found` |

The public JSON request and response types are defined in `src/CarRental.Contracts/RentalContracts.cs`. The API maps these public contracts to the internal domain model and does not expose the internal `Rental` type directly.