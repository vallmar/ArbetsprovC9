# Customer API documentation

This document describes the HTTP API exposed by the car-rental SaaS.

The API is the integration boundary for customers. Customer applications should treat the HTTP/JSON models documented here as the public contract and should not depend on internal Domain or Application models.

## Authentication and tenant identity

Every customer request to the rental API must use a valid bearer access token.

For this showcase, the API contains a deliberately small local token endpoint:

```http
POST /oauth/token
Content-Type: application/json
```

```json
{
  "clientId": "tenant-a",
  "clientSecret": "secret-a"
}
```

The response is:

```json
{
  "accessToken": "<signed-jwt>",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

The token is then sent on customer API calls:

```http
Authorization: Bearer <accessToken>
```

The API validates the JWT signature, issuer, audience and lifetime. The tenant identity is derived from the trusted `client_id` claim. Customers must never put a tenant identifier in the rental JSON payload.

The local `/oauth/token` endpoint exists only to make this repository self-contained. It is a showcase token issuer, not a production identity provider. A real deployment would use an OAuth 2.0/OIDC identity provider and the API would validate tokens issued by that provider.

Invalid or missing bearer tokens result in `401 Unauthorized` with `WWW-Authenticate: Bearer`.

## Tenant isolation

Tenant isolation is part of the application behaviour:

- the same booking number may exist in different tenants;
- rental lookups are always scoped to the authenticated tenant;
- a tenant cannot return or modify another tenant's rental;
- an attempt to access another tenant's known booking returns `404 Not Found`, so the API does not disclose that the booking exists for another tenant;
- the application logs a security-relevant warning when it detects such a cross-tenant access attempt.

The external response deliberately does not reveal the owning tenant.

## Base URL

The examples use:

```text
http://localhost:5000
```

In a deployed environment this is replaced by the customer's SaaS API URL.

## Response and error handling

The most important rule for a customer integration is:

> **Check the HTTP status code first. Then inspect the response body.**

Business/application errors use this JSON shape:

```json
{
  "error": "Booking number is already in use."
}
```

The `error` value is intended to be understandable to a human. Customers should not use the English error message as a machine-readable error code.

Malformed JSON or framework-level failures may have a different response shape. Customers should therefore handle the HTTP status code first and retain the response body for diagnostics.

## POST /oauth/token

Demo token endpoint used by the repository's customer examples.

### Request

```json
{
  "clientId": "tenant-a",
  "clientSecret": "secret-a"
}
```

Two showcase clients are configured:

| Client ID | Demo secret | Tenant |
|---|---|---|
| `tenant-a` | `secret-a` | tenant-a |
| `tenant-b` | `secret-b` | tenant-b |

### Success

HTTP `200 OK`.

```json
{
  "accessToken": "<signed-jwt>",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

### Failure

HTTP `401 Unauthorized` when the client credentials are not accepted.

## POST /api/rentals/pickup

Registers a vehicle pickup.

### Request

```http
POST /api/rentals/pickup
Authorization: Bearer <accessToken>
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
| `bookingNumber` | string | Yes | Customer's booking identifier. Unique within the authenticated tenant. |
| `registrationNumber` | string | Yes | Vehicle registration number. |
| `customerIdentifier` | string | Yes | Identifier for the customer making the rental. |
| `category` | string | Yes | `SmallCar`, `Combi`, or `Truck`. |
| `pickupTime` | ISO-8601 timestamp | Yes | Time at which the vehicle was picked up. |
| `pickupOdometer` | integer | Yes | Odometer reading at pickup. Must not be negative. |

### Success

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

The response also contains a `Location` header pointing to `/api/rentals/{bookingNumber}`.

### Errors

- `401 Unauthorized` — missing or invalid access token.
- `400 Bad Request` — request or business rule rejected.

Typical business error:

```json
{
  "error": "Booking number is already in use."
}
```

## POST /api/rentals/{bookingNumber}/return

Registers the return of a rental and calculates the final price.

### Request

```http
POST /api/rentals/TEST-001/return
Authorization: Bearer <accessToken>
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
| `bookingNumber` | path string | Yes | Booking to return within the authenticated tenant. |
| `returnTime` | ISO-8601 timestamp | Yes | Return time. Cannot be before pickup time. |
| `returnOdometer` | integer | Yes | Return odometer. Cannot be below pickup odometer. |
| `baseDailyPrice` | decimal | Yes | Base daily rental price. Must not be negative. |
| `baseKmPrice` | decimal | Yes | Base kilometre price. Must not be negative. |

### Success

HTTP `200 OK`.

```json
{
  "bookingNumber": "TEST-001",
  "finalPrice": 1000
}
```

### Errors

- `401 Unauthorized` — missing or invalid access token.
- `404 Not Found` — booking does not exist for the authenticated tenant, including when another tenant owns it.
- `400 Bad Request` — rental exists but the return violates a business rule.

Example `404`:

```json
{
  "error": "Rental 'TEST-001' was not found."
}
```

The `404` response is intentionally identical whether the booking does not exist or belongs to another tenant.

Typical `400` messages include:

- `Rental has already been returned.`
- `Return time cannot be before pickup time.`
- an argument error for an invalid odometer value;
- an argument error for invalid pricing.

## Customer integration flow

```text
Customer application
       |
       | POST /oauth/token
       | client credentials
       v
   access token
       |
       | Authorization: Bearer <JWT>
       v
    Rental API
       |
       +--> validate JWT
       |
       +--> client_id -> tenant context
       |
       +--> tenant-scoped rental operation
       |
       +--> 2xx / 4xx response
```

The customer application owns its own persistence and UI. It only depends on the HTTP API and its documented JSON contracts.

## Security and logging

The API never logs bearer access tokens or client secrets.

When the API detects that an authenticated tenant requested a booking owned by another tenant, it emits a structured Warning for operators. The caller still receives `404 Not Found` so the existence or ownership of the booking is not disclosed.

The security log contains internal diagnostic context such as the requesting tenant, booking number, and owning tenant. This context is for service operators and is not part of the customer response.

## Contract summary

| Endpoint | Success | Errors |
|---|---:|---|
| `POST /oauth/token` | `200 OK` | `401 Unauthorized` |
| `POST /api/rentals/pickup` | `201 Created` | `400 Bad Request`, `401 Unauthorized` |
| `POST /api/rentals/{bookingNumber}/return` | `200 OK` | `400 Bad Request`, `401 Unauthorized`, `404 Not Found` |

The public rental request/response types are defined in `src/CarRental.Contracts/RentalContracts.cs`. Tenant identity and JWT details are authentication concerns and are not part of those JSON rental DTOs.
