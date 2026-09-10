# Backend Directives: ElectronicLive .NET API

## Tech Stack
- .NET 9 Web API (`ElectronicLive.sln`, `src/ElectronicLive.Api`)
- Architecture: Aggregator / BFF (No internal DB; pulls from external EDM/gig sources)
- Resilience & Networking: `IHttpClientFactory` with Polly policies
- Testing: xUnit, FluentAssertions, WireMock.NET (`tests/`)

## Commands
- Build: `dotnet build backend/ElectronicLive.sln`
- Run API: `dotnet run --project backend/src/ElectronicLive.Api`
- Run Tests: `dotnet test backend/ElectronicLive.sln`

## Architecture & Code Boundaries
- **Encapsulated Clients**: Place external source calls under `src/ElectronicLive.Api/Infrastructure/Clients/`. Each external vendor/API gets its own typed client interface.
- **DTOs**: Separate raw upstream models (`External/`) from exposed API contracts (`Contracts/` or `Responses/`). Never expose raw third-party schemas directly.
- **Async Execution**: Use `Task.WhenAll` to fan-out and query independent gig sources concurrently. Always accept and forward `CancellationToken`.

## Guardrails
- NEVER instantiate `new HttpClient()`. Use typed clients via dependency injection.
- NEVER create database migrations or introduce an ORM.
- Handle external upstream failures gracefully; a failure from one gig provider should not crash the entire endpoint.
