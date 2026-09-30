# Frontend Directives: ElectronicLive React Client

> [!NOTE]
> For UI mockups, layout rules, and phased execution steps, reference `docs/SPECIFICATION.md` and `docs/IMPLEMENTATION_PLAN.md` only when needed for implementation context.

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
- **API Client Layer (`src/api/client.ts`):** All HTTP requests must be encapsulated in typed functions within `src/api/client.ts`. Never execute raw `fetch()` or `axios` in components or hooks.
- **Single Target:** All client fetch requests must target the local .NET Web API. Never query external third-party vendor APIs directly from the browser.
- **Queries (`src/hooks/` or `queryOptions`):** Encapsulate all `GET` queries and cache-orchestration in dedicated hooks under `src/hooks/` using TanStack Query v5 object syntax.
- **Mutations (`useMutation`):**
  - **Shared / Cache-Invalidating:** Encapsulate in `src/hooks/` when mutations invalidate queries (`queryClient.invalidateQueries`) or are shared across multiple views.
  - **Isolated / Single-View:** Inline `useMutation` directly in parent container/dialog components when single-use with zero query cache side-effects (avoids redundant 1-line wrapper hooks).
- **Form & Mutation Error Synchronization:** When a form displays a mutation error banner (`mutation.error`), always reset the mutation state (`mutation.reset()`) inside the input `onChange` handler so stale server errors do not remain frozen on screen while the user is actively typing a correction.
- **Domain-Organized Components:** Organize UI components by feature domain under `components/<domain>/`, with shared primitives under `components/common/` and global chrome under `components/layout/`.
- **Pure Leaf Components & Container Boundaries:** Presentational leaf components (e.g. buttons, rows, badges, forms) must remain pure, deterministic functions (props in -> JSX out). Keep server state, caching, and query orchestration isolated in parent container views (such as pages or dialog wrappers) or dedicated custom hooks.
- **Native `<dialog>` Modal Management:** Trigger modals using imperative `.showModal()` on mount and handle dismissal via native events / backdrop clicks. Never pass `open={isOpen}` as a JSX attribute on `<dialog>`, as it marks the element open before `showModal()` runs and prevents native top-layer modal behavior, backdrop styling, and focus traps.
- **Resilience UI:** Views consuming asynchronous data must cleanly render:
  1. Loading skeleton states gated strictly on active network fetching (`fetchStatus === 'fetching'` or `isFetching`). Never gate skeletons on `isPending` when `enabled` can be false (in TanStack Query v5, `isPending` is true for unexecuted queries).
  2. Partial or empty data states with an explicit idle state (`const isIdle = !query.trim()`).
  3. Network error boundaries with user-friendly retry actions.
- **Timezone-Safe Date Parsing:** Never parse `"YYYY-MM-DD"` date strings using `new Date(...)` with local getters (`getDate()`, `getDay()`), which causes day-shifts in non-UTC timezones. Parse components directly (`dateStr.split('-')`) or use UTC getters (`getUTCDate()`, `getUTCMonth()`).

## React 19 & TypeScript Conventions
- **No `React.FC`:** Define components using standard function syntax with explicit typed interfaces:
  ```tsx
  interface Props {
    title: string;
  }
  export function Component({ title }: Props) { ... }
  ```
- **React 19 Native Refs:** Pass `ref` directly as a standard component prop; do not wrap components in `forwardRef` (deprecated in React 19).
- **Zero Derived State in `useEffect`:** Never sync props to state or compute derived values inside `useEffect`. Calculate derivations synchronously during render.
- **Controlled Event Triggers:** Only trigger data fetches on explicit user submissions (form submit, button click, or preset selection). Never trigger network requests on input `onChange`.
- **Strict Typing:** DO NOT use the `any` type or unsafe `as` type assertions.

## Styling & Accessibility Guardrails
- **Tailwind Utility Discipline:** All styling must strictly use Tailwind utility classes. DO NOT create custom `.css` or `.module.css` stylesheets.
- **Mobile Touch Targets:** Interactive controls (buttons, inputs, selectables) must provide a minimum tap target height of 44px (`min-h-[44px]`).
- **Semantic HTML & A11y:** Use semantic elements (`<header>`, `<main>`, `<section>`, `<article>`, `<button type="button">`, `<input type="search">`). All icon-only interactive controls must declare an explicit `aria-label`.

## Testing Standards
- **Test Structure (Pattern B):** All unit and component tests must be placed in dedicated `__tests__/` subdirectories within their corresponding domain or component folders (e.g., `src/components/events/__tests__/EventRow.test.tsx`). Keep component directory listings clean and free of spec files.
- **Test Naming:** All test cases in Vitest must strictly follow the naming pattern of `Should_...` (e.g., `it('Should_render_results_when_query_succeeds', ...)`).
- **Tooling:** Use Vitest + React Testing Library + `@testing-library/user-event`. Enzyme is strictly forbidden.
- **User-Centric Queries:** Always query the DOM via Testing Library user-facing roles (`getByRole`, `getByLabelText`). Never query by CSS class names, element IDs, or DOM hierarchy.
- **Realistic Events:** Use `@testing-library/user-event` rather than `fireEvent` to simulate realistic browser interactions.
- **Contract Mocks via MSW:** Test data fetching hooks against MSW network handlers. Do not manually mock `fetch` using `vi.fn()`.

## Guardrails
- Build UI controls directly using Tailwind CSS primitives to avoid runtime CSS-in-JS bloat. For complex accessible widgets, prefer headless primitives (Radix UI) styled with Tailwind rather than monolithic opinionated suites (MUI, Chakra, AntD).
- NEVER render empty or null `href` on ticket buttons; if `ticketUrl` is missing or empty, render an unclickable disabled state (`Tickets TBA`).

## Comment Policy (Why, Never What)
- Write clean, self-documenting code with expressive naming so comments are rarely needed. Strictly forbid tautological comments (e.g., `// render row`, `// call api`, `// set state`). Comments are only permitted to explain the "why"—such as workarounds for third-party browser quirks (e.g., popup blocker behavior on external ticket links), non-obvious date formatting edge cases, or upstream vendor payload anomalies. Delete boilerplate comments immediately.
