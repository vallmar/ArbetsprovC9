# Customer integration examples

These projects intentionally represent customers rather than parts of the SaaS core.

Both examples integrate through the public HTTP API and obtain a JWT access token from the showcase `/oauth/token` endpoint before calling rental endpoints.

## Customer A

`CustomerA.JsonConsole` demonstrates the smallest possible integration: a console UI, customer-owned JSON persistence and an HTTP client for the SaaS API.

## Customer B

`CustomerB.Postgres` demonstrates a customer with a relational database. Its database schema and data model are owned by the customer application. PostgreSQL is started with `docker compose up -d` from that directory.

Neither customer project references `CarRental.Domain` or `CarRental.Application`.
