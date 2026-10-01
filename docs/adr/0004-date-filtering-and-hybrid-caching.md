# ADR 0004: Date Range Filtering and Hybrid Aggregator Caching

## Status
Accepted

## Context
Users require the ability to discover events across specific date windows (e.g., "Tonight", "This Weekend", "Next Weekend", "Next 30 Days", or a custom calendar range). Previously, event discovery in `ElectronicLive.Api` only accepted `query` (free-text artist/venue) and `genre`, requiring at least one to be populated. 

External ticketing providers exhibit disparate querying and data-volume characteristics:
1. **Ticketmaster**: Supports ISO-8601 date range parameters (`startDateTime`, `endDateTime`).
2. **Skiddle**: Supports date range parameters (`minDate`, `maxDate`).
3. **Resident Advisor**: Uses a GraphQL text search index requiring non-empty keyword input with no structured date arguments.

Querying broad geographic regions (e.g. London) without dates returns tens of thousands of listings, overwhelming upstream rate limits and memory. Conversely, querying specific artists yields small result sets (typically 3–10 gigs/year) across all dates.

## Decision
1. **API Contract & Validation**:
   - Extend `GET /api/events/search` with optional `from` and `to` query parameters formatted as ISO `DateOnly` (`YYYY-MM-DD`).
   - Allow date-only queries (empty `query` and `genre`) constrained to a maximum window of 7 days (`to.DayNumber - from.DayNumber <= 7`) to prevent unbounded city-wide crawls.
   - Enforce `to >= from`.
   - Dateless events (`Date == null`) are strictly excluded when a date filter is active.

2. **Hybrid Caching & Execution Architecture**:
   - **Artist / Genre Queries:** Retain canonical cache keys (`events:agg:{city}:q={query}:g={genre}`). Upstream providers fetch all upcoming events for the entity, and `EventSearchService` filters by `from`/`to` in-memory. This prevents cache fragmentation when toggling between date presets.
   - **Date-Only Queries:** Construct date-scoped cache keys (`events:agg:{city}:date:{from}:{to}`). Forward date parameters directly to Ticketmaster and Skiddle. Gracefully bypass Resident Advisor when both `query` and `genre` are absent.
   - **Upstream Resilience & Documented Quirk:** Resident Advisor's client will return `[]` immediately when both `query` and `genre` are empty, accompanied by an explicit code comment explaining why (RA's GraphQL search index requires a keyword search term and provides no date range filtering).

3. **Client Architecture & UI Layout**:
   - Represent date filters orthogonally in `useEventSearchState` as concrete ISO strings (`from` and `to`), maintaining exact 1:1 parity with the backend API contract and URL address bar (`?from=YYYY-MM-DD&to=YYYY-MM-DD`).
   - **Visual Hierarchy:** Place the `DateFilterBar` directly below `SearchBar` and above `QuickPills` (Date First), prioritizing timing filters for nightlife users.
   - **Quick Presets:** Provide quick preset pills: `Tonight`, `This Weekend` (Fri–Sun), `Next Weekend`, `Next 30 Days`, and `Custom...`.
   - **Custom Range Tray:** Clicking "Custom..." smoothly expands an inline responsive tray below the pills with native HTML5 `<input type="date">` inputs (`From`, `To`, `Apply`, `Cancel`), preventing mobile viewport clipping and zero extra JavaScript bundle overhead.
   - **Filter Interactions:** Toggling an active date preset pill clears the date filter; clearing the text search preserves active date bounds.

## Consequences
- **Cache Hit Ratio:** High cache reuse for popular artists across different date slices.
- **Upstream Protection:** City-wide queries are strictly bounded to 7 days, avoiding upstream timeouts and rate limits.
- **Provider Isolation:** Resident Advisor is safely isolated from date-only queries without returning empty/failing aggregator responses.
- **Client Bundle:** Zero external calendar library dependencies added to the frontend bundle.
