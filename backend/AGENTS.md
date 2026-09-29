# Backend Directives: ElectronicLive .NET API

## Tech Stack
- .NET 10 Web API (`ElectronicLive.sln`, `src/ElectronicLive.Api`)
- Pattern: Minimal APIs organized by resource extension classes (`Endpoints/`) returning `TypedResults`
- Architecture: Aggregator / BFF with Relational Persistence for Watchlists (PostgreSQL + EF Core) & external EDM/gig provider aggregation
- Persistence: PostgreSQL (Neon.tech), Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- Resilience & Networking: `IHttpClientFactory` with Polly policies
- Testing: xUnit, Shouldly, NSubstitute 

## Architecture & Code Boundaries
- **Endpoints Over Controllers:** Map endpoints using static extension methods on `IEndpointRouteBuilder` inside `Endpoints/` (e.g., `Endpoints/EventEndpoints.cs`). Never place full endpoint implementations in `Program.cs`.
- **Unit-Testable Handlers:** Endpoint logic must reside in `internal static` handler methods so they can be unit-tested directly without spinning up HTTP test servers. `[InternalsVisibleTo]` must target `ElectronicLive.Api.UnitTests`.
- **Encapsulated Clients:** Place external source calls under `src/ElectronicLive.Api/Infrastructure/Clients/` (or `Services/`). Each external vendor/API gets its own typed client interface (e.g., `ITicketmasterClient`).
- **DTOs & Schema Separation:** Separate raw upstream third-party models (`External/`) from exposed API contracts (`Models/` or `Contracts/`). Never expose raw third-party schemas directly to callers.
- **Async Execution:** Always accept and forward `CancellationToken`. Use `Task.WhenAll` when querying multiple independent gig providers concurrently.
- When writing tests, please follow the naming pattern of `Should_....`


## Guardrails
- Handle external upstream failures gracefully; a failure from one gig provider should not crash the entire endpoint.

## Azure Safeguards
- Always prompt for explicit user confirmation before executing destructive or state-altering Azure commands (e.g., `az * delete`, resource teardown, scale-down, or state-altering scripts).
- Verify the active subscription and tenant context (`az account show`) before executing modifications if multiple subscriptions are configured.


- **Pragmatic SOLID Design:** 
  - **Single Responsibility (SRP):** Classes and endpoints must have one clear reason to change (e.g., separate HTTP routing from external third-party integration).
  - **Dependency Inversion (DIP):** Depend on abstractions (`ITicketmasterClient`) for external boundaries rather than concrete implementations.
  - **Interface Segregation (ISP):** Keep service interfaces focused on specific capabilities rather than monolithic "catch-all" contracts.
  - **Pragmatic Methods:** Prefer clear, linear, top-to-bottom method flow that fits on a single screen over premature extraction of single-use private helpers. Only extract private methods for reused logic, deep nesting, or isolated, branch-heavy mappings.

- **Comment Policy (Why, Never What):** Write clean, self-documenting code with expressive naming so comments are rarely needed. Strictly forbid tautological comments (e.g., `// call api`, `// set variable`). Comments are only permitted to explain the "why"—such as workarounds for third-party API quirks, non-obvious framework traps (e.g., .NET URI path stripping), or regulatory/RFC specifications. Delete boilerplate framework comments immediately.
