# Backend Directives: ElectronicLive .NET API

## Commands
- Dev Server: `dotnet run --project src/ElectronicLive.Api`
- Run Tests: `dotnet test`
- Watchlist Scanner: `dotnet run --project src/ElectronicLive.Api -- --job scan-watchlist`
- Migrations: `dotnet ef database update --project src/ElectronicLive.Api`

## Architecture & Boundaries
- **Endpoints Over Controllers:** Map Minimal APIs in `Endpoints/` returning `TypedResults`. Handlers are HTTP adapters only (`internal static` covered by `[InternalsVisibleTo]`); delegate domain and DB logic to `Services/`.
- **External Clients:** External providers live under `Clients/` implementing domain abstractions (`IEventProvider`) with Polly resilience. Never expose raw upstream schemas to callers.
- **Embedded Templates:** Email templates in `Background/Email/Templates/` must be compiled as `<EmbeddedResource>`.
- **Persistence:** Dual SQLite (local `electroniclive.db`) and PostgreSQL (Neon cloud) via EF Core.
- **Async & Cancellation:** Always accept and forward `CancellationToken`.

## Style & Coding Standards
- **Returns & Primitives:** Return records or enums; never multi-element tuples. No artificial `Result<T>` wrappers for simple CRUD.
- **Named Arguments:** Use named arguments for booleans, nulls, and adjacent same-type primitives (`fromId: a, toId: b`); avoid on self-evident single-arg calls.
- **Comments (Why, Never What):** Forbid tautological comments (`// call api`, `// set state`). Only comment the "why" (vendor API quirks, non-obvious URI bugs).
- **XML Docs:** Never generate `/// <summary>` boilerplate for internal code (`CS1591` is suppressed). Document endpoints fluently on route mappings.
- **No Test Visibility Widening:** Never make methods `public` purely to facilitate unit tests. Keep helpers `internal static`.

## Testing Standards
- Runner: xUnit with Shouldly and NSubstitute. Test names must follow `Should_...`.
- Maintain shared factories in `TestHelpers/` (e.g. `EventTestFactory`). Assert observable state/side effects over mock internals.
