# Backend Directives: ElectronicLive .NET API

## Commands
- Dev Server: `dotnet run --project src/ElectronicLive.Api` (inside `/backend`)
- Run Tests: `dotnet test`
- Watchlist Scanner: `dotnet run --project src/ElectronicLive.Api -- --job scan-watchlist`
- Database Migrations: `dotnet ef database update --project src/ElectronicLive.Api`

## Architecture & Code Boundaries
- **Endpoints Over Controllers:** Map endpoints via static extension methods in `Endpoints/` returning `TypedResults`. Route handlers are HTTP transport adapters only; delegate domain and database orchestration to `Services/`. Handlers use `internal static` methods testable directly via `[InternalsVisibleTo]`.
- **External Clients:** Integrations (Ticketmaster, Skiddle, Resident Advisor) reside under `src/ElectronicLive.Api/Clients/` implementing domain abstractions (`IEventProvider`, `IArtistVerificationService`) with Polly resilience. Never expose raw upstream schemas to API callers.
- **Background & Notifications:** Scheduled scanner and email dispatchers reside in `Background/` (`WatchlistScannerService`, `IEmailDispatcher`). Notification templates in `Background/Email/Templates/` must be compiled as `<EmbeddedResource>`.
- **Persistence:** Dual SQLite (local development: `electroniclive.db`) and PostgreSQL (Neon cloud) via EF Core.
- **Validation & Errors:** Validate request syntax on DTOs returning `TypedResults.ValidationProblem()`. Keep business checks in domain services using early returns.

## Testing Standards
- Runner: xUnit with Shouldly and NSubstitute.
- Test Naming: Every test case must follow the naming pattern `Should_...`.
- Test Helpers: Maintain shared test fixtures in `TestHelpers/` (e.g. `EventTestFactory`). Assert observable state and side effects over mock internals.
