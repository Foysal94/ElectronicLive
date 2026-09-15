# Frontend Directives: ElectronicLive React Client

## Tech Stack
- **Framework:** React 19, TypeScript (Strict Mode)
- **Build Tool:** Vite (configured with dev server proxy to local .NET API)
- **Server State & Caching:** TanStack Query v5 (React Query)
- **Styling:** Tailwind CSS
- **Testing:** Vitest, React Testing Library, `@testing-library/user-event`, MSW (Mock Service Worker)

## Commands
- Dev Server: `npm run dev` (run inside `/client`)
- Build: `npm run build`
- Run Tests: `npm run test`
- Lint & Typecheck: `npm run lint && npm run typecheck`

## Architecture & Code Boundaries
- **Single Target:** All fetch requests target the local .NET Web API (`/api/events/search`). Never make external third-party vendor requests directly from the client.
- **Dedicated Data Hooks:** Wrap all query logic inside custom hooks in `src/hooks/` (e.g., `useEventsSearch.ts`) using TanStack Query v5 object syntax (`useQuery({ queryKey, queryFn })`). Never embed fetch calls or `useQuery` directly inside UI components.
- **Domain-Organized Components:** Keep UI components organized by domain:
  - `components/layout/` (Header, navigation chrome)
  - `components/search/` (SearchBar, QuickPills)
  - `components/events/` (EventList, EventRow, DateBlock, ProviderButton, Skeletons)
  - `components/common/` (Badge, ErrorBoundary)
- **Pure Leaf Components:** Components in `components/events/` and `components/common/` must be pure and deterministic (props in -> JSX out). Keep all state/query orchestration in parent views or hooks.
- **Resilience UI:** Views must cleanly handle and render:
  1. Animated loading skeleton states matching the timetable row layout.
  2. Empty zero-results feedback states explaining that no gigs were found across the supported providers.
  3. Network error boundaries with user-friendly retry actions.

## React 19 & TypeScript Conventions
- **No `React.FC`:** Define components using standard function syntax with explicit typed interfaces:
  ```tsx
  interface Props {
    title: string;
  }
  export function EventTitle({ title }: Props) { ... }
  ```
- **React 19 Native Refs:** Pass `ref` directly as a component prop when needed; do not wrap components in `forwardRef` (deprecated in React 19).
- **Zero Derived State in `useEffect`:** Never sync props to state or compute derived values inside `useEffect`. Perform derivations synchronously during render.
- **Controlled Search Submission:** Only trigger event queries on explicit form submission (Enter key, "Search" button click, or quick-pill click). Never trigger API queries on input `onChange` to protect upstream API rate limits.
- **Semantic HTML & Accessibility:** Use semantic elements (`<header>`, `<main>`, `<section>`, `<article>`, `<button type="button">`, `<input type="search">`). All icon-only actions must provide an explicit `aria-label`.

## Styling & Layout Guardrails
- **Tailwind Utility Discipline:** All styles must use Tailwind utility classes. DO NOT create custom `.css` or `.module.css` stylesheets.
- **Mobile Touch Target Minimums:** All interactive controls (pills, ticket buttons) must have a minimum tap height of 44px (`min-h-[44px]`).
- **Responsive Multi-Provider Layout:** Event ticket buttons must gracefully scale across mobile viewports:
  - 1 provider: full-width button.
  - 2 providers: 50/50 side-by-side split.
  - 3 providers: 2-row wrap (2 on top row, 1 full-width on bottom).
  - 4 providers: 2x2 grid.
  - 5+ providers: top 2 providers visible with an overflow drawer.

## Testing Standards
- **Test Naming:** All test cases in Vitest must strictly follow the naming pattern of `Should_...` (e.g. `it('Should_render_events_when_query_succeeds', ...)`).
- **Tooling:** Use Vitest + React Testing Library + `@testing-library/user-event`. NEVER use Enzyme (dead/unsupported).
- **User-Centric Queries:** Always query the DOM via Testing Library user-facing roles (`getByRole`, `getByLabelText`). Never query by CSS class names, element IDs, or DOM hierarchy.
- **Realistic Events:** Use `@testing-library/user-event` rather than `fireEvent` to simulate realistic browser interactions.
- **Contract Mocks via MSW:** Test data fetching hooks against MSW network handlers. Do not manually mock `fetch` using `vi.fn()`.

## Guardrails
- DO NOT use the `any` type or unsafe `as` type assertions in application code.
- DO NOT use raw `fetch()` or `axios` inside `useEffect`.
- DO NOT add unapproved third-party UI component libraries (e.g., MUI, AntD, Chakra); build clean primitives with Tailwind CSS.

## Comment Policy (Why, Never What)
- Write clean, self-documenting code with expressive naming so comments are rarely needed. Strictly forbid tautological comments (e.g., `// render row`, `// call api`, `// set state`). Comments are only permitted to explain the "why"—such as workarounds for third-party browser quirks (e.g., popup blocker behavior on external ticket links), non-obvious date formatting edge cases, or upstream vendor payload anomalies. Delete boilerplate comments immediately.
