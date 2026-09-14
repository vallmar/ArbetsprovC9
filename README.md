# Car Rental SaaS Reference Architecture

This repository is a reference implementation of the car-rental candidate assignment.

The point is not to build a large rental system. The point is to keep the rental business rules small, testable and independent from customer UI and persistence choices.

## Architecture

```text
 Customer A Console ──HTTP──┐
                            │
 Customer B App ─────HTTP───┤
                            ▼
                       Rental API
                            │
                      Application
                            │
                          Domain
                            │
                    Persistence Port
                            │
                    ┌───────┴────────┐
                    ▼                ▼
                JSON/File       PostgreSQL
                 adapter          adapter
```

The important boundary is the HTTP API. Customer applications are consumers of the product; they do not reference the domain assembly. Persistence is an application port, so the core does not know which database is used.

## Main projects

- `src/CarRental.Domain` - rental aggregate, state rules, categories and pricing data.
- `src/CarRental.Application` - use cases, price calculation and persistence port.
- `src/CarRental.Infrastructure` - reference in-memory persistence adapter for the API.
- `src/CarRental.Api` - thin HTTP adapter.
- `tests/CarRental.Tests` - xUnit tests for business behaviour.

## Customer examples

The customer examples are intentionally separate applications. In a real SaaS deployment they could live in completely separate repositories.

- `customers/CustomerA.JsonConsole` - console frontend + local JSON persistence to illustrate the simplest customer integration.
- `customers/CustomerB.Postgres` - console/API client + PostgreSQL persistence to illustrate a more conventional relational customer environment.

They should communicate with the SaaS API via HTTP and map their own storage models to/from API contracts.

## Design choices

- No UI framework is required by the core.
- No database technology is required by the core.
- The API is the product boundary for customer frontends.
- `IRentalRepository` is a port; concrete adapters belong outside the core.
- xUnit tests focus on business behaviour rather than implementation details.
- No CQRS, MediatR, event sourcing or elaborate tenant framework has been added because the assignment does not justify them.

## Assignment assumptions

See `docs/ASSUMPTIONS.md`. The original assignment explicitly allows assumptions where the specification is unclear.

## Run

```bash
dotnet test

dotnet run --project src/CarRental.Api
```
