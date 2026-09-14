# Backend Directives: ElectronicLive .NET API

## Tech Stack
- .NET 9 Web API (`ElectronicLive.sln`, `src/ElectronicLive.Api`)
- Pattern: Minimal APIs organized by resource extension classes (`Endpoints/`) returning `TypedResults`
- Architecture: Aggregator / BFF (No internal DB; pulls from external EDM/gig sources)
- Resilience & Networking: `IHttpClientFactory` with Polly policies
- Testing: xUnit, Shouldly, NSubsitiutte 

## Commands
- Build: `dotnet build backend/ElectronicLive.sln`
- Run API: `dotnet run --project backend/src/ElectronicLive.Api`
- Run Tests: `dotnet test backend/ElectronicLive.sln`

## Architecture & Code Boundaries
- **Endpoints Over Controllers:** Map endpoints using static extension methods on `IEndpointRouteBuilder` inside `Endpoints/` (e.g., `Endpoints/EventEndpoints.cs`). Never place full endpoint implementations in `Program.cs`.
- **Unit-Testable Handlers:** Endpoint logic must reside in `internal static` handler methods so they can be unit-tested directly without spinning up HTTP test servers. `[InternalsVisibleTo]` must target `ElectronicLive.Api.UnitTests`.
- **Encapsulated Clients:** Place external source calls under `src/ElectronicLive.Api/Infrastructure/Clients/` (or `Services/`). Each external vendor/API gets its own typed client interface (e.g., `ITicketmasterClient`).
- **DTOs & Schema Separation:** Separate raw upstream third-party models (`External/`) from exposed API contracts (`Models/` or `Contracts/`). Never expose raw third-party schemas directly to callers.
- **Async Execution:** Always accept and forward `CancellationToken`. Use `Task.WhenAll` when querying multiple independent gig providers concurrently.
- When writing tests, please follow the naming pattern of `Should_....`


## Guardrails
- NEVER instantiate `new HttpClient()`. Use typed clients via dependency injection.
- NEVER create database migrations or introduce an ORM.
- Handle external upstream failures gracefully; a failure from one gig provider should not crash the entire endpoint.
- Keep methods linear and focused: Prefer straightforward, top-to-bottom method flow that fits on one screen over premature extraction of small, single-use private helpers. Only extract private methods when logic is reused, has deep nesting, or represents complex mapping/parsing routines.

- **Pragmatic SOLID Design:** 
  - **Single Responsibility (SRP):** Classes and endpoints must have one clear reason to change (e.g., separate HTTP routing from external third-party integration).
  - **Dependency Inversion (DIP):** Depend on abstractions (`ITicketmasterService`) for external boundaries rather than concrete implementations.
  - **Interface Segregation (ISP):** Keep service interfaces focused on specific capabilities rather than monolithic "catch-all" contracts.
  - **Pragmatic Methods:** Prefer clear, linear, top-to-bottom method flow that fits on a single screen over premature extraction of single-use private helpers. Only extract private methods for reused logic, deep nesting, or isolated, branch-heavy mappings.
