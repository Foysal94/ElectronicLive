# Backend Directives: ElectronicLive .NET API

## Tech Stack
- .NET 10 Web API (`ElectronicLive.sln`, `src/ElectronicLive.Api`)
- Pattern: Minimal APIs organized by resource extension classes (`Endpoints/`) returning `TypedResults`
- Architecture: Aggregator / BFF with Relational Persistence for Watchlists (PostgreSQL + EF Core) & external EDM/gig provider aggregation
- Persistence: PostgreSQL (Neon.tech), Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- Resilience & Networking: `IHttpClientFactory` with Polly standard resilience pipelines
- Testing: xUnit, Shouldly, NSubstitute 

## Architecture & Code Boundaries
- **Endpoints Over Controllers:** Map endpoints using static extension methods on `IEndpointRouteBuilder` inside `Endpoints/` (e.g., `Endpoints/EventEndpoints.cs`, `Endpoints/SubscriptionEndpoints.cs`). Never place full endpoint implementations in `Program.cs`.
- **No Direct Persistence in Endpoints:** Endpoints are strictly HTTP transport adapters (routing, model binding, returning `TypedResults`). Never execute raw `DbContext` queries in `Endpoints/`. Delegate all database and domain orchestration to `Services/` so logic is isolated and reusable by background jobs (`Background/`).
- **Unit-Testable Handlers:** Endpoint logic must reside in `internal static` handler methods so they can be unit-tested directly without spinning up HTTP test servers. `[InternalsVisibleTo]` must target `ElectronicLive.Api.UnitTests`.
- **Encapsulated Clients:** Place external provider integrations under `src/ElectronicLive.Api/Clients/` (implementing domain abstractions like `IEventProvider`, `IArtistVerificationService`). Each external vendor gets its own typed client and resilience policies.
- **Core Domain Services:** Synchronous business capabilities and aggregators reside under `src/ElectronicLive.Api/Services/` (e.g., `IEventSearchService`, `ISubscriptionService`, `IArtistVerificationService`).
- **Pragmatic Return Types:** Return domain records or simple status flags directly; do not create artificial `Result<T>`, `StatusEnum`, or `Contracts/` wrapper layers for straightforward domain operations.
- **Layered Validation & Guard Clauses:** Validate request syntax on DTOs (e.g., `TryValidate()`) returning `TypedResults.ValidationProblem()` immediately; keep semantic and business checks inside domain services using flat early-return guard clauses.
- **Background & Notification Pipelines:** Background processing, scheduled jobs (ACA Jobs), and email dispatching reside under `src/ElectronicLive.Api/Background/` (e.g., `Background/Email/` for `IEmailDispatcher`, `ResendEmailDispatcher`, `LoggingEmailDispatcher`, `EmailTemplateBuilder` and `Background/Scanner/` for `WatchlistScannerService`).
- **Embedded Resource Templates:** Email and HTML notification templates must be stored under `src/ElectronicLive.Api/Background/Email/Templates/` and compiled as an `<EmbeddedResource>` in the `.csproj` to prevent runtime `FileNotFoundException` path failures in containerized (Docker/ACA) environments.
- **DTOs & Schema Separation:** Separate raw upstream third-party models (`Clients/*/Models.cs`) from exposed API contracts (`Models/`). Never expose raw third-party schemas directly to callers.
- **One Type Per File (`SA1649`):** Never declare records, DTOs, or enums inside interface files or leak private loop types. Every public/internal type gets its own dedicated file named after the type.
- **Feature-Scoped Models:** Root `Models/` is strictly for public HTTP API contracts across endpoint boundaries. Client and feature DTOs stay flat in their feature root (e.g., `Clients/*/Models.cs`, `ScanResult.cs`); no nested `Models/` folders unless 5+ DTOs.
- **Records Over Tuples:** Use immutable `record` or `enum` types for method returns—never multi-element tuples (e.g., `(int, int, int)`).
- **No Test-Driven Visibility Widening:** Never widen method access modifiers (e.g., making methods `public` or `public static`) purely to facilitate unit tests. Keep pure algorithmic helpers `internal static` (covered by `[InternalsVisibleTo]`), and test orchestration through public service interfaces.
- **Async Execution:** Always accept and forward `CancellationToken`. Use `Task.WhenAll` when querying multiple independent gig providers concurrently.
- **Testing Conventions:** All test methods must strictly follow the naming pattern `Should_....` Never duplicate manual entity or DTO construction across test files; maintain shared test factories in `TestHelpers/` (e.g., `EventTestFactory`) and assert observable state/side-effects rather than internal mock mechanics.

## Guardrails
- Handle external upstream failures gracefully; a failure from one gig provider or email recipient must not crash the entire endpoint or abort processing for other subscribers.
- **Date Filtering & Hybrid Caching Architecture:**
  - **Hybrid Date Caching:** When queries include an artist `query` or `genre`, cache the full canonical schedule in `HybridCache` using cache key `events:agg:{city}:q={query}:g={genre}` and perform date window slicing in-memory. For date-only queries (without query or genre), isolate cache keys by date bounds (`events:agg:{city}:date:{from}:{to}`) and pass bounds directly upstream.
  - **7-Day Validation Cap:** Enforce a strict 7-day maximum window (`to - from <= 7`) for date-only queries in endpoint validation (`EventEndpoints.cs`) to prevent upstream rate limiting and unbounded fan-out. Return RFC 7807 `TypedResults.ValidationProblem()` on violations. Context-enriched queries (`query` or `genre`) permit wider spans (up to 30+ days).
  - **Resident Advisor Date-Only Bypass:** Resident Advisor's GraphQL search index requires a keyword search term (`searchTerm`) and does not support date-range-only scans. `ResidentAdvisorClient` must immediately return an empty list (`[]`) without executing network requests when neither `query` nor `genre` is provided, allowing Ticketmaster and Skiddle to serve the date window.

## Azure Safeguards
- Always prompt for explicit user confirmation before executing destructive or state-altering Azure commands (e.g., `az * delete`, resource teardown, scale-down, or state-altering scripts).
- Verify the active subscription and tenant context (`az account show`) before executing modifications if multiple subscriptions are configured.

## Pragmatic SOLID Design
- **Single Responsibility (SRP):** Classes and endpoints must have one clear reason to change (e.g., separate HTTP routing from background dispatching and external provider integration).
- **Dependency Inversion (DIP):** Depend on abstractions (`IEventProvider`, `IArtistVerificationService`, `IEmailDispatcher`) for external boundaries rather than concrete implementations.
- **Interface Segregation (ISP):** Keep service interfaces focused on specific capabilities rather than monolithic "catch-all" contracts.
- **Pragmatic Methods:** Prefer clear, linear, top-to-bottom method flow that fits on a single screen over premature extraction of single-use private helpers. Only extract private methods for reused logic, deep nesting, or isolated, branch-heavy mappings.

## Comment Policy (Why, Never What)
- Write clean, self-documenting code with expressive naming so comments are rarely needed. Strictly forbid tautological comments (e.g., `// call api`, `// set variable`). Comments are only permitted to explain the "why"—such as workarounds for third-party API quirks, non-obvious framework traps (e.g., .NET URI path stripping), or regulatory/RFC specifications. Delete boilerplate framework comments immediately.
