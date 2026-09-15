# ElectronicLive Client Specification

## 1. Overview & Product Mission

**ElectronicLive** is a focused web utility designed to discover upcoming electronic dance music (EDM) and live gigs across London venues by artist, event title, or venue. It operates as a Single-Page Application (SPA) backed by a decoupled .NET Minimal API aggregator that merges and deduplicates event data across **Resident Advisor**, **Ticketmaster**, and **Skiddle**.

### Core UX Principles
- **Utility-First & Lean:** Fast, responsive, and stripped of marketing clutter, carousels, and advertisements.
- **Data-First Hierarchy:** Search is a compact, focused tool; the event timetable occupies over 80% of the active viewport.
- **Chronological Orientation:** Events are displayed in strict chronological order with prominent date blocks for instant scanning.
- **One-Click Purchasing:** Side-by-side direct ticketing links with real-time availability badges. Zero nested menus or accordion clicks.

---

## 2. Approved UI Design

The finalized visual design utilizes a mid-tone slate grey palette with high-contrast typography, interactive quick-search pills, and a chronological timetable row list.

![ElectronicLive Desktop UI Mockup](./docs/ui-mockup.jpg)

### Palette & Visual Tokens
- **Base Background:** `#181b1f` (Dark slate grey, avoiding pitch black)
- **Elevated Surfaces / Rows:** `#22262d` (Zinc/charcoal container surface)
- **Row Hover State:** `#2b3039`
- **Border Lines:** `rgba(255, 255, 255, 0.08)` (Subtle 1px outlines for depth)
- **Primary Typography:** High-contrast neutral white (`#f3f4f6`)
- **Muted Typography / Meta:** Slate grey (`#9ca3af`)
- **Status Indicator (On Sale):** Emerald / mint green pill (`bg-emerald-950 text-emerald-400 border-emerald-800`)
- **Status Indicator (Sold Out):** Rose / red pill (`bg-rose-950 text-rose-400 border-rose-800`)

---

## 3. Component Architecture & Hierarchy

```
client/src/
├── api/
│   ├── client.ts                 # Typed fetch client targeting local .NET backend
│   └── types.ts                  # Domain contracts (EventResponse, EventTicketOffer, EventStatus, EventProvider)
├── components/
│   ├── common/
│   │   ├── Badge.tsx             # Reusable pill badge (Status, City, Provider)
│   │   ├── BrandLogo.tsx         # Inline SVG audio soundwave mark + typography
│   │   └── ErrorBoundary.tsx     # React error boundary with fallback banner
│   ├── layout/
│   │   ├── Header.tsx            # Brand mark, City scope badge, and Provider attribution
│   │   └── Container.tsx         # Responsive max-width wrapper
│   ├── search/
│   │   ├── SearchBar.tsx         # Text input with submit button and clear action
│   │   ├── QuickPillGroup.tsx    # Accessible horizontal pill container
│   │   └── QuickPills.tsx        # Pre-configured Artists & Venues pill rows
│   └── events/
│       ├── EventList.tsx         # Container rendering event rows or empty/skeleton states
│       ├── EventRow.tsx          # Chronological timetable row item
│       ├── DateBlock.tsx         # High-contrast calendar badge (Day, Date, Month)
│       ├── ProviderButton.tsx    # Direct outbound vendor ticket button
│       ├── EventSkeleton.tsx     # Pulsing skeleton rows for loading state
│       └── EmptyState.tsx        # Zero-results feedback card
├── features/
│   └── events/
│       ├── useEventsSearch.ts    # TanStack Query custom hook for search execution & caching
│       └── eventUtils.ts         # Date formatting, provider sorting, and URL helpers
├── App.tsx                       # Root view orchestrator
└── main.tsx                      # QueryClientProvider and DOM mount
```

---

## 4. Detailed Functional Specifications

### 4.1. Header & Provider Attribution
- **Brand Identity:** Minimalist geometric soundwave SVG icon paired with bold sans-serif wordmark `ElectronicLive`.
- **Scope Chip:** Distinct pill reading `📍 London, UK`.
- **Provider Attribution:** Subtitle located directly beneath the title:
  > `Aggregating live events from Resident Advisor · Ticketmaster · Skiddle`
  - Explains the data perimeter upfront so users understand why closed or proprietary platforms (such as DICE) are omitted.

### 4.2. Unified Search Section
- **Unified Query:** Single search input accepting artist names (e.g., *"Amelie Lens"*), event titles (e.g., *"A State Of Trance"*), or venue names (e.g., *"Drumsheds"*).
- **Trigger Mechanics:**
  - Fires on **Enter key** or clicking the **"Search"** button.
  - **No keystroke debouncing:** Live querying on keystroke is explicitly forbidden to prevent spamming upstream third-party rate limits.
- **Categorized Quick-Search Pills:**
  - **Quick Search Artists:** `Hardwell`, `Armin van Buuren`, `Amelie Lens`, `Charlotte de Witte`, `Bicep`, `Eric Prydz`.
  - **Quick Search Venues:** `Drumsheds`, `Fabric`, `FOLD`, `Ministry of Sound`, `Studio 338`.
  - Clicking any pill immediately populates the search input and executes the query.

### 4.3. Results Timetable (Concept A)
- **Chronological Sorting:** Gigs are ordered ascending by date and door time. Unannounced dates appear at the end.
- **Row Anatomy:**
  1. **Date Block (Left):** Prominent day-of-week abbreviation (e.g., `SAT`), numerical date (`14`), and uppercase month (`NOV`).
  2. **Event & Venue Details (Center):**
     - Primary headline: Artist / Event title in bold white.
     - Secondary metadata: Venue name prefixed with map pin icon `📍`.
  3. **Direct Ticketing Actions (Right):**
     - Side-by-side buttons for every provider holding tickets for this gig (e.g., `[ Resident Advisor ↗ ]` and `[ Skiddle ↗ ]`).
     - Micro-badge above each button displaying availability (`On Sale`, `Sold Out`, `Postponed`).
     - Clicking opens the external vendor checkout in a new window/tab (`target="_blank"`, `rel="noopener noreferrer"`).

### 4.4. State Machine & Resilience UI
1. **Initial / Idle State:**
   - Display search bar and quick-search pills centered comfortably on screen.
   - Timetable list remains empty until a search is initiated.
2. **Loading State:**
   - Render 4–6 animated skeleton rows ([`EventSkeleton`](file:///Users/foysalahmed/Code/ElectronicLive/client/src/components/events/EventSkeleton.tsx)) mimicking the exact date block and row height to prevent layout shifts.
3. **Zero-Results State:**
   - Centered card informing the user:
     > *"No upcoming London gigs found for '[query]' across Resident Advisor, Ticketmaster, or Skiddle."*
   - Includes quick suggestions to try one of the curated artist or venue pills.
4. **Network / Server Error State:**
   - Amber/Red warning banner indicating connection failure:
     > *"Unable to reach the London events service. Please ensure the backend API is running or try again."*
   - Includes a `"Retry"` button to re-trigger the TanStack Query execution.

---

## 5. Technical Stack & Implementation Guardrails

Per [`client/AGENTS.md`](file:///Users/foysalahmed/Code/ElectronicLive/client/AGENTS.md):

| Layer | Technology | Directives |
| :--- | :--- | :--- |
| **Framework** | React 19 + TypeScript | Strict Mode enabled. The `any` type is strictly forbidden. |
| **Build & Dev Tool** | Vite | Dev server proxy configured for `/api` $\rightarrow$ `http://localhost:5247`. |
| **State & Fetching** | TanStack Query v5 | Custom hooks only; raw `fetch()` or `axios` in `useEffect` is forbidden. |
| **Styling** | Tailwind CSS v4 | Pure utility classes with semantic color tokens. |
| **Testing** | Vitest + RTL + MSW | Mock Service Worker simulates .NET API payloads. Test naming: `Should_...`. |

---

## 6. Points of Concern, Trade-offs & Edge Cases

1. **Third-Party Upstream Latency & Timeouts:**
   - The backend queries 3 separate third-party platforms concurrently (`Task.WhenAll`). Response times can fluctuate between 200ms and 1500ms.
   - *Mitigation:* Aggressive TanStack Query caching (`staleTime: 5 * 60 * 1000` — 5 minutes) so re-clicking popular pills (e.g. *Fabric*, *Hardwell*) returns instantly from cache without hitting the backend.
2. **Mobile Viewport Wrapping:**
   - On narrow screens (< 640px), horizontal rows with multi-provider buttons risk horizontal overflow.
   - *Mitigation:* Responsive flex layout: row items stack vertically on mobile (Date Block on top, Event Details in middle, full-width Provider Buttons at bottom), transitioning to horizontal rows on desktop (`sm:flex-row`).
3. **Missing or Incomplete Dates/Times:**
   - Upstream providers occasionally publish events without confirmed start times (e.g., festival dates or TBA shows).
   - *Mitigation:* Date block falls back gracefully to `TBA` when `Date` is null, avoiding broken layout or parsing crashes.
4. **Venue Name Discrepancies:**
   - Different providers format venue names differently (e.g., *"The Drumsheds"*, *"Drumsheds London"*, *"Drumsheds UK"*).
   - *Mitigation:* Backend deduplication handles normalization, but client typography must truncate smoothly (`truncate` / `text-ellipsis`) to avoid breaking row height on lengthy names.
5. **CORS & Local Development:**
   - Directly fetching from port `5173` (Vite) to `5247` (.NET) causes browser CORS preflight blocks unless properly configured.
   - *Mitigation:* Vite proxy in `vite.config.ts` transparently rewrites `/api` requests to `http://localhost:5247`, eliminating CORS issues during development.

---

## 7. Post-MVP Extensibility Roadmap

The component structure is intentionally prepared for the following additions without core refactoring:
- **Filter Toolbar:** Dedicated seam between Quick Pills and Results for date range chips (*"This Weekend"*, *"Next 30 Days"*).
- **Personal Watchlist:** Header slot reserved for `[ Watchlist (count) ]` drawer trigger; [`EventRow`](file:///Users/foysalahmed/Code/ElectronicLive/client/) has reserved action slot for a bookmark icon.
- **View Switcher:** `EventList` can easily support a toggle between `Timetable List` and `Poster Grid` if event artwork is added to the backend in the future.
