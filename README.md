# Car Rental SaaS Reference Architecture

This repository is a reference implementation of a car-rental SaaS.

The point is not to build a large rental system. The point is to keep the rental business rules small, testable and independent from customer UI and persistence choices while demonstrating realistic API, authentication and tenant-isolation boundaries.

## Architecture

```text
 Customer A Console ──HTTP──┐
                            │
 Customer B App ─────HTTP───┤
                            ▼
                       Rental API
                            │
                 JWT authentication
                            │
                       TenantContext
                            │
                      Application
                            │
                          Domain
                            │
                    Persistence Port
                            │
                    ┌───────┴────────┐
                    ▼                ▼
                In-memory       Customer DBs
                  demo          owned by customers
```

The important boundary is the HTTP API. Customer applications are consumers of the product; they do not reference the domain assembly. Persistence is an application port, so the core does not know which database is used.

## Authentication and multi-tenancy

Customer API calls use standard JWT bearer authentication:

```http
Authorization: Bearer <access_token>
```

The API validates the JWT and derives the tenant from its trusted `client_id` claim. Rental persistence is scoped by `(TenantId, BookingNumber)`.

For a self-contained showcase, the API includes a tiny `/oauth/token` endpoint that issues demo JWTs for `tenant-a` and `tenant-b`. This is intentionally not a production identity provider; a real deployment would use an OAuth 2.0/OIDC provider.

If an authenticated tenant requests a booking owned by another tenant, the API returns `404 Not Found` without disclosing the booking's existence. The application logs the blocked cross-tenant attempt as a security-relevant warning.

See `docs/CUSTOMER_API.md` for the complete HTTP contract.

## Main projects

- `src/CarRental.Domain` - rental aggregate, state rules, categories and pricing data.
- `src/CarRental.Application` - use cases, price calculation, tenant context and persistence port.
- `src/CarRental.Infrastructure` - reference in-memory persistence adapter for the API.
- `src/CarRental.Api` - HTTP, JWT authentication and composition boundary.
- `src/CarRental.Contracts` - public HTTP/JSON contracts.
- `tests/CarRental.Tests` - xUnit tests for business behaviour, API contracts, authentication, tenant isolation and observability.

## Customer examples

The customer examples are intentionally separate applications. In a real SaaS deployment they could live in completely separate repositories.

- `customers/CustomerA.JsonConsole` - console frontend + local JSON persistence.
- `customers/CustomerB.Postgres` - console/API client + PostgreSQL persistence.

They communicate with the SaaS API via HTTP, obtain JWT access tokens, and map their own storage models to/from API contracts.

## Design choices

- No UI framework is required by the core.
- No database technology is required by the core.
- The API is the product boundary for customer frontends.
- `IRentalRepository` is a port; concrete adapters belong outside the core.
- JWT authentication is handled at the API boundary; the Application layer only sees `ITenantContext`.
- Cross-tenant access attempts are logged without exposing the other tenant to the caller.
- No CQRS, MediatR, event sourcing or elaborate tenant framework has been added because the project does not justify them.

## Assignment assumptions

See `docs/ASSUMPTIONS.md`. The original assignment explicitly allows assumptions where the specification is unclear.

## Run

```bash
dotnet test
dotnet run --project src/CarRental.Api
```

The development JWT signing key is in `appsettings.Development.json` and is deliberately a non-production showcase key. Replace it with a real secret/key-management solution for any real deployment.
