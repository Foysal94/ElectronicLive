# Specification: Date Range Filtering & Hybrid Event Discovery

## Status: ready-for-agent

## Problem Statement

London electronic music fans looking for upcoming gigs and club nights face severe friction when trying to discover events happening at specific times. Users often know *when* they want to go out (e.g. "Tonight", "This Weekend", "Next Weekend", or during a specific calendar window) before or in conjunction with *who* they want to see.

Currently, the ElectronicLive application only supports searching by free-text artist/venue query or standardized electronic genre pills, both of which require an open-ended date scan. Users cannot filter events by date, causing high cognitive fatigue when sifting through future listings, and preventing impulse or planned weekend clubbing discovery.

## Solution

Introduce comprehensive date range filtering across both the .NET aggregation backend and the React web client:

1. **Quick Date Presets:** Enable one-click discovery for the primary London nightlife timeframes: `Tonight`, `This Weekend` (Friday–Sunday), `Next Weekend`, and `Next 30 Days`.
2. **Custom Date Selection:** Provide an anchored popover dropdown powered by `react-datepicker` (`selectsRange`, `inline`) and `date-fns` for visual calendar date range selection.
3. **Orthogonal Filter Composition:** Allow combining date filters with existing artist/venue searches and genre pills without wiping active criteria.
4. **Standalone Date Browsing:** Allow discovering London events by date alone without entering an artist or genre (capped at a 7-day maximum window).
5. **Hybrid Aggregator Caching:** For artist/genre queries, cache full schedules in `HybridCache` and slice dates in memory; for broad date-only queries, forward date bounds directly to upstream ticketing providers (Ticketmaster and Skiddle).
6. **URL Deep-Linking:** Synchronize concrete `from` and `to` ISO strings (`YYYY-MM-DD`) in the browser URL for bookmarking and sharing.

## User Stories

1. As a clubber planning a night out on Friday, I want to click "This Weekend" so that I instantly see all electronic music events happening between Friday and Sunday without typing search queries.
2. As a music fan searching for a specific DJ (e.g. "Bicep"), I want to filter by "Next 30 Days" so that I only see their shows happening in the coming month.
3. As a techno fan, I want to select the "Techno" genre pill and "This Weekend" date preset together so that I see techno nights happening this coming weekend.
4. As a visitor coming to London for a specific trip, I want to enter custom "From" and "To" dates so that I can see events scheduled during my exact visit.
5. As a user who accidentally filtered by date, I want to click the active date preset pill again to toggle it off and return to viewing all upcoming events.
6. As a user searching for an artist within a date range, I want to clear the search bar text without losing my selected date range so that I can easily search for another artist in the same timeframe.
7. As a user browsing a date range, I want events that do not have a confirmed date (`Date: null`) to be excluded from the timetable so that I am not misled by unscheduled listings.
8. As a user sharing a link with friends, I want the URL to contain `?from=YYYY-MM-DD&to=YYYY-MM-DD` so that everyone sees the exact same calendar dates regardless of when they open the link.
9. As a mobile and desktop user, I want the custom date picker to open an anchored dropdown with a visual calendar grid so that I can easily select and preview my date range without clunky multi-input form steps.
10. As a system operator, I want date-only discovery queries to be limited to at most 7 days so that upstream ticket providers are never overloaded with massive city-wide event crawls.
11. As a system operator, I want upstream ticketing APIs that do not support date filtering (Resident Advisor) to be bypassed during date-only queries so that the aggregator does not fail or return low-quality results.

## Implementation Decisions

### 1. Backend Architecture & Aggregation Boundary
- The event search HTTP endpoint accepts optional `from` and `to` query parameters formatted as ISO `DateOnly` (`YYYY-MM-DD`).
- Model validation enforces:
  - Valid `YYYY-MM-DD` date syntax.
  - `to >= from`.
  - If both `query` and `genre` are missing, `(to - from).Days <= 7`. Rejects requests violating these bounds with `TypedResults.ValidationProblem`.
- When filtering by date, events with `Date == null` are strictly filtered out.
- Hybrid caching strategy:
  - When `query` or `genre` is provided: Query cache key remains `events:agg:{city}:q={query}:g={genre}`. Upstream providers fetch all upcoming events for that entity, and `EventSearchService` filters by `from`/`to` in memory.
  - When `query` and `genre` are absent: Query cache key is date-scoped: `events:agg:{city}:date:{from}:{to}`. Date ranges are passed directly to upstream providers.
- Provider client adaptations:
  - Ticketmaster: Append `&startDateTime={from}T00:00:00Z&endDateTime={to}T23:59:59Z`.
  - Skiddle: Append `&minDate={from:yyyy-MM-dd}&maxDate={to:yyyy-MM-dd}`.
  - Resident Advisor: Return empty list immediately when both `query` and `genre` are missing, accompanied by an explicit code comment explaining the search index keyword requirement.

### 2. Frontend Architecture & State Boundary
- The API client contract (`fetchEvents`) accepts optional `from` and `to` parameters.
- Search state hook (`useEventSearchState`) manages `activeFrom` and `activeTo` as orthogonal state properties parsed from and serialized to `window.location.search`.
- Visual hierarchy:
  - `SearchBar` (top)
  - `DateFilterBar` (dedicated second row with preset pills: `Tonight`, `This Weekend`, `Next Weekend`, `Next 30 Days`, and `Custom...`)
  - `QuickPills` (third row with Artists, Venues, Genres)
  - `EventList` (timetable timetable results)
- Custom Date selection: Clicking `Custom...` expands an anchored dropdown card beneath the button containing `react-datepicker` in range mode, a live date range badge, an `Apply` button, a `Cancel` button, and a `Clear` button.
- Interaction rules:
  - Clicking an active preset pill deselects it and clears the date parameters.
  - Clicking `Clear` in the search bar clears the search text while keeping the active date filter intact.

## Testing Decisions

### Seam Architecture (Highest Feasible Seams)
- **Backend HTTP Transport Seam:** Unit-test `EventEndpoints.SearchEvents` directly using `internal static` handlers, verifying parameter extraction, `ValidationProblem` status codes, and delegation.
- **Backend Domain Service Seam:** Unit-test `EventSearchService.SearchEventsAsync` against provider stubs, verifying in-memory date slicing, cache key isolation, and date-only provider forwarding.
- **Frontend Hook Seam:** Test `useEventSearchState` using `@testing-library/react` (`renderHook`), asserting URL query parameter serialization, preset date calculation, and clearing logic.
- **Frontend Component Seam:** Test `DateFilterBar` and `App` using `@testing-library/react` and `@testing-library/user-event`, asserting user-visible button roles, active class toggling, inline tray expansion, and custom date submission.

## Out of Scope

- Time-of-day filtering (e.g. afternoon vs. late-night clubbing hours).
- Recurring calendar subscriptions (e.g. "Notify me about any techno show every Friday").
- Multi-city date selection (city remains London by default).
- Heavy monolithic UI design systems (e.g. Ant Design, MUI).

## Further Notes

- References: [`docs/adr/0004-date-filtering-and-hybrid-caching.md`](file:///Users/foysalahmed/Code/ElectronicLive/docs/adr/0004-date-filtering-and-hybrid-caching.md) and [`CONTEXT.md`](file:///Users/foysalahmed/Code/ElectronicLive/CONTEXT.md).
