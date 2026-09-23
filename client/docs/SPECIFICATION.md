# ElectronicLive Client Specification

## 1. Overview & Product Mission

**ElectronicLive** is a focused web utility designed to discover upcoming electronic dance music (EDM) and live gigs across London venues by artist, event title, or venue. It operates as a Single-Page Application (SPA) backed by a decoupled .NET Minimal API aggregator that merges and deduplicates event data across **Resident Advisor**, **Ticketmaster**, and **Skiddle**.

### Core UX Principles
- **Utility-First & Lean:** Fast, responsive, and stripped of marketing clutter, carousels, and advertisements.
- **Data-First Hierarchy:** Search is a compact, focused tool; the event timetable occupies over 80% of the active viewport.
- **Chronological Orientation:** Events are displayed in strict chronological order with prominent date blocks for instant scanning.
- **One-Click Purchasing:** Side-by-side direct ticketing links with real-time availability badges. Zero nested menus or accordion clicks.

---

## 2. Approved UI Designs (Desktop & Mobile)

The visual design utilizes a mid-tone slate grey palette with high-contrast typography, interactive quick-search pills, and a chronological timetable row list.

### Desktop Layout
![ElectronicLive Desktop UI Mockup](./ui-mockup.jpg)

### Mobile Layout
![ElectronicLive Mobile UI Mockup](./ui-mockup-mobile.jpg)

### Palette & Visual Design Tokens
- **Base Background:** `#181b1f` (Dark slate grey, avoiding pitch black)
- **Elevated Surfaces / Cards:** `#22262d` (Zinc/charcoal container surface)
- **Hover State:** `#2b3039`
- **Border Lines:** `rgba(255, 255, 255, 0.08)` (Subtle 1px outlines for depth)
- **Primary Typography:** High-contrast neutral white (`#f3f4f6`)
- **Muted Typography / Meta:** Slate grey (`#9ca3af`)
- **Status Indicator (On Sale):** Emerald green pill (`bg-emerald-950 text-emerald-400 border-emerald-800`)
- **Status Indicator (Sold Out):** Rose red pill (`bg-rose-950 text-rose-400 border-rose-800`)

---

## 3. Final Component Architecture & Hierarchy

```
client/src/
├── api/
│   ├── client.ts                 # Typed fetch client targeting /api/events/search
│   └── types.ts                  # Domain models (EventResponse, EventTicketOffer, EventStatus, EventProvider)
├── components/
│   ├── common/                   # Reusable UI primitives
│   │   ├── Badge.tsx             # Status badge (On Sale, Sold Out)
│   │   └── ErrorBoundary.tsx     # Network and runtime error boundary banner
│   ├── layout/                   # Global page frame
│   │   └── Header.tsx            # Soundwave SVG logo, London chip, and Provider attribution
│   ├── search/                   # Search controls
│   │   ├── SearchBar.tsx         # Text input with submit/clear buttons
│   │   └── QuickPills.tsx        # Quick Search Artists, Venues, and Genres pill rows
│   └── events/                   # Timetable display
│       ├── EventList.tsx         # Results container (orchestrates loading, empty, and data states)
│       ├── EventRow.tsx          # Timetable row item (responsive desktop row / mobile card)
│       ├── DateBlock.tsx         # High-contrast calendar badge (Day, Date, Month)
│       ├── ProviderButton.tsx    # Direct outbound vendor ticket button with availability badge
│       ├── EventSkeleton.tsx     # Pulsing skeleton rows during network fetch
│       └── EmptyState.tsx        # Zero-results feedback card
├── hooks/
│   └── useEventsSearch.ts        # TanStack useQuery hook for searching and caching
├── App.tsx                       # Main view orchestrator
└── main.tsx                      # QueryClientProvider & React root mount
```

### Architectural Rationale
- **Domain-Organized Components:** Categorizing UI components by domain (`layout/`, `search/`, `events/`, `common/`) keeps files easily discoverable and prevents merge conflicts across agents.
- **Dedicated Data Hook (`hooks/useEventsSearch.ts`):** Encapsulates TanStack Query's caching (`staleTime: 5 mins`), race condition handling, and query key management. Presentational components remain pure and receive plain props.

---

## 4. Detailed Functional Specifications

### 4.1. Header & Provider Attribution
- **Brand Identity:** Minimalist geometric soundwave SVG icon paired with bold sans-serif wordmark `ElectronicLive`.
- **Scope Chip:** Distinct pill reading `📍 London, UK`.
- **Provider Attribution:** Subtitle located directly beneath the title:
  > `Aggregating live events from Resident Advisor · Ticketmaster · Skiddle`
  - Explains data scope upfront so users understand why proprietary or closed platforms (such as DICE) are omitted.

### 4.2. Search Section, Genre Taxonomies & URL Deep-Linking
- **Dual Query Capabilities:**
  - **Free-Text Search (`query`):** Free-text query parameter accepting artist names (e.g., *"Amelie Lens"*), event titles (e.g., *"A State Of Trance"*), or venue names (e.g., *"Drumsheds"*).
  - **Genre Taxonomy Search (`genre`):** Dedicated enum query parameter (`techno`, `house`, `drum-and-bass`, `trance`, `garage`) targeting upstream musical classifications and club event codes.
  - **Validation Rule:** The backend endpoint (`GET /api/events/search`) requires at least one of `query` or `genre`. If both are omitted or empty, returns `400 Bad Request` with validation details.
- **Provider Taxonomy Mapping Rules:**
  | Genre | Ticketmaster Mapping | Skiddle Mapping | Resident Advisor Mapping |
  | :--- | :--- | :--- | :--- |
  | `techno` | `classificationName=Music&keyword=Techno` | `eventcode=CLUB&g=4` | GraphQL `searchTerm: "Techno"` |
  | `house` | `classificationName=Music&keyword=House` | `eventcode=CLUB&g=1` | GraphQL `searchTerm: "House"` |
  | `drum-and-bass` | `classificationName=Music&keyword=Drum and Bass` | `eventcode=CLUB&g=7` | GraphQL `searchTerm: "Drum and Bass"` |
  | `trance` | `classificationName=Music&keyword=Trance` | `eventcode=CLUB&g=5` | GraphQL `searchTerm: "Trance"` |
  | `garage` | `classificationName=Music&keyword=UK Garage` | `eventcode=CLUB&g=26` | GraphQL `searchTerm: "Garage"` |
- **Trigger Mechanics:**
  - Fires on **Enter key** or clicking the **"Search"** button.
  - **No keystroke debouncing:** Live querying on keystroke is explicitly forbidden to prevent spamming upstream third-party rate limits.
- **Categorized Quick-Search Pills:**
  - **Quick Search Artists:** `Hardwell`, `Armin van Buuren`, `Amelie Lens`, `Charlotte de Witte`, `Bicep`, `Eric Prydz`.
  - **Quick Search Venues:** `Drumsheds`, `Fabric`, `FOLD`, `Ministry of Sound`, `Studio 338`.
  - **Quick Search Genres:** `Techno`, `House`, `Drum & Bass`, `Trance`, `Garage`.
  - Pills are styled as distinct clickable buttons with rounded borders and dark container fills (`min-h-[44px]` touch target).
  - Clicking an Artist or Venue pill populates the search input and queries `?query=...`.
  - Clicking a Genre pill queries `?genre=...`, bypassing free-text matching and isolating upstream music categories.
- **URL Synchronization (Deep-Linking):**
  - Synchronize active search state to the browser URL using native `URLSearchParams` and `window.history.replaceState`.
  - Free-text searches sync to `?q=...`.
  - Genre pill filters sync to `?genre=...` using clean kebab-case tokens (e.g. `?genre=drum-and-bass`), avoiding ampersand serialization quirks.
  - On page load, initialize search state from either `?genre=` or `?q=` and execute the corresponding query.

### 4.3. Results Timetable & Intermediate Viewport Layout
- **Chronological Sorting:** Gigs are ordered ascending by date and door time. Unannounced dates appear at the end.
- **Desktop Layout (`>= 640px`):**
  - Full-width horizontal row layout.
  - Left column: High-contrast date block (`SAT 14 NOV`).
  - Center column: Artist / Event title in bold white; Venue name with location pin `📍`.
  - Right column: Direct ticket provider buttons (`[ Resident Advisor ↗ ]`, `[ Skiddle ↗ ]`) with availability status pills.
  - Intermediate Viewports (`640px`–`1024px`): Provider buttons wrap gracefully (`flex-wrap gap-2`) so the artist/venue title is never squeezed into an unreadable sliver on tablets.
  - Single-click checkout opening in a new tab (`target="_blank"`, `rel="noopener noreferrer"`).
  - Defensive Ticket Links: If `ticketUrl` is null or empty, `ProviderButton` renders an unclickable disabled badge (`Tickets TBA`).

---

## 5. Mobile Responsive Design & Multi-Provider Scaling

Mobile viewports (`< 640px`) present unique ergonomic constraints that must be adhered to:

### 5.1. Touch Targets & Card Layout
- **Minimum Touch Target:** All interactive pill buttons and ticket buttons must have a minimum tap height of **44px** (`min-h-[44px]`).
- **Stacked Card Anatomy:**
  - **Top Row:** Compact date block positioned immediately beside the Event and Venue title.
  - **Bottom Row:** Ticket provider buttons span across the bottom of the card for easy thumb access.

### 5.2. Multi-Provider Scaling Matrix (Mobile)
The backend integrates exactly three providers (`ResidentAdvisor`, `Ticketmaster`, `Skiddle`) and merges duplicates with `.DistinctBy(o => o.Provider)`. The mobile layout handles all real-world combinations without dead code:

| Provider Count | Mobile Layout Rule | Visual Representation |
| :--- | :--- | :--- |
| **1 Provider** (~85% of gigs) | Single full-width button (`w-full`) | `[ Get Tickets on Resident Advisor ↗ (On Sale) ]` |
| **2 Providers** (~12% of gigs) | 50/50 side-by-side split (`grid grid-cols-2 gap-2`) | `[ Resident Advisor ↗ ]` `[ Skiddle ↗ ]` |
| **3 Providers** (All 3 vendors) | 2-row wrap: Row 1 has 2 buttons (50/50); Row 2 has 1 full-width button | Row 1: `[ RA ↗ ]` `[ Skiddle ↗ ]`<br>Row 2: `[ Ticketmaster ↗ (Full Width) ]` |

---

## 6. State Machine & Resilience UI

1. **Initial / Idle State:**
   - Search bar and quick-search pills displayed cleanly.
   - Results container renders an inviting prompt:
     > *"Select a London artist or club above, or search to view upcoming shows."*
   - Avoids firing unprompted network calls on cold load, preserving upstream rate limits.
2. **Loading State:**
   - 4–6 animated skeleton rows ([`EventSkeleton`](file:///Users/foysalahmed/Code/ElectronicLive/client/src/components/events/EventSkeleton.tsx)) mirroring the row layout to eliminate layout shift.
   - Gated strictly on active network fetching (`fetchStatus === 'fetching'`). Never gate on `isPending`.
3. **Zero-Results State:**
   - Centered feedback card stating:
     > *"No upcoming London gigs found for '[query]' across Resident Advisor, Ticketmaster, or Skiddle."*
   - Includes suggestions to try one of the curated artist or venue pills.
4. **Network / Server Error State:**
   - Warning banner indicating connection failure with a `"Retry"` button:
     > *"Unable to reach the London events service. Please ensure the backend API is running or try again."*

---

## 7. Points of Concern, Trade-offs & Edge Cases

1. **Timezone-Safe Date Parsing:** Backend sends C# `DateOnly` as `"YYYY-MM-DD"`. Naive `new Date("YYYY-MM-DD")` evaluates as UTC midnight and shifts backward by one day in negative UTC offsets (e.g. UTC-5). `DateBlock` must parse components directly (`dateStr.split('-')`) or use UTC getters (`getUTCDate()`, `getUTCMonth()`).
2. **Unannounced / Missing Ticket Links:** Upstream providers occasionally list shows before ticket sales open (`ticketUrl: null`). `ProviderButton` must render a disabled state (`Tickets TBA`) instead of empty `href` attributes.
3. **Third-Party Upstream Latency:** Aggregator queries 3 APIs concurrently. Caching with TanStack Query (`staleTime: 5 * 60 * 1000`) avoids redundant requests when toggling between pills.
4. **Missing Start Times:** Date block gracefully renders `TBA` when an event lacks a confirmed date or time.
5. **Venue Name Truncation:** Long venue strings truncate with ellipsis (`truncate`) to prevent breaking card boundaries on narrow screens.
6. **CORS in Development:** Local Vite dev server proxies `/api` calls directly to `http://localhost:5275`. Backend also enforces CORS policy in production.
7. **Upstream Keyword vs. Taxonomy Disambiguation & RA Fallback:** Free-text `query` searches match against artist, title, and venue names. To prevent generic genre queries (such as *"House"* or *"Garage"*) from matching non-electronic venues (e.g. *"House of Vans"*, *"The Garage"* in Highbury), musical genres are queried via the dedicated `genre` parameter. This applies category constraints (`classificationName=Music` on Ticketmaster and `eventcode=CLUB` with Skiddle genre IDs). Since Resident Advisor has no dedicated genre field in GraphQL, RA queries `searchTerm` using specific genre tokens (`"UK Garage"`, `"Drum and Bass"`).
8. **Clean URL State & Parameter Separation:** Rather than serializing ambiguous genre phrases with special characters into `?q=Drum+%26+Bass`, genre filters sync to `?genre=drum-and-bass` using URL-safe kebab-case strings. Free-text search continues to sync via `?q=...`.
9. **Mobile Viewport Vertical Real Estate:** Adding a third row of pills expands the search filter container height. To preserve the core principle that the event timetable occupies over 80% of the active viewport on mobile, pill rows retain compact flex wrapping (`gap-2 sm:gap-3`) and touch-friendly dimensions (`min-h-[44px]`).
10. **Backend Cache Key Collision Vulnerability:** To prevent cache collisions when callers pass both `query` and `genre` (e.g. `?query=Bicep&genre=techno` vs `?genre=techno`), `HybridCache` keys must be formatted unambiguously as `events:{city}:q={query}:g={genre}` rather than using coalescing fallbacks (`genre ?? query`).
11. **Skiddle URI Construction & Relevance Filter Guard:** When `genre` is set and `query` is empty, `SkiddleClient` must omit the `&keyword=` parameter (avoiding `Uri.EscapeDataString(null)` errors) and ensure `MatchesQuery` returns `true` for empty queries so valid genre listings are never discarded.

---

## 8. Post-MVP Extensibility Roadmap

- **Filter Toolbar Seam:** Slot reserved between Quick Pills and Results for date range chips (*"This Weekend"*, *"Next 30 Days"*).
- **Personal Watchlist:** Header slot reserved for `[ Watchlist (count) ]` drawer trigger; [`EventRow`](file:///Users/foysalahmed/Code/ElectronicLive/client/) has reserved action slot for a bookmark icon.
- **View Switcher:** `EventList` can support toggling between `Timetable List` and `Poster Grid` if event images are added upstream.
