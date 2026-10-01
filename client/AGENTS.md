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
- **Server State & Mutation Scope:** Encapsulate `GET` queries and cache-invalidating mutations in custom hooks under `src/hooks/` using TanStack Query v5 object syntax. Inline single-use mutations with zero query cache side-effects directly in parent container/dialog views to avoid 1-line wrapper hook proliferation.
- **Pure Leaf Components & Container Boundaries:** Presentational leaf components (e.g. buttons, rows, badges, forms) must remain pure, deterministic functions (props in -> JSX out). Keep server state, caching, and mutation orchestration isolated in parent container views or custom hooks.
- **Domain-Organized Components:** Organize UI components by feature domain under `components/<domain>/`, with shared primitives under `components/common/` and global chrome under `components/layout/`.
- **Resilience UI:** Views consuming asynchronous data must cleanly render:
  1. Loading skeleton states gated strictly on active network fetching (`fetchStatus === 'fetching'` or `isFetching`), never `isPending`.
  2. Partial or empty data states with an explicit idle state (`const isIdle = !query.trim()`).
  3. Network error boundaries with user-friendly retry actions.
- **Timezone-Safe Date Parsing:** Never parse `"YYYY-MM-DD"` date strings using `new Date(...)` with local getters. Parse components directly (`dateStr.split('-')`) or use UTC getters (`getUTCDate()`, `getUTCMonth()`).

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
- **Strict Typing:** DO NOT use the `any` type or unsafe `as` type assertions.

## Styling & Accessibility Guardrails
- **Pragmatic Component Selection & Tailwind Discipline:** All styling must strictly use Tailwind utility classes without custom `.css` stylesheets.
  - **No Monolithic Suites:** DO NOT import heavy, opinionated design systems (e.g., Ant Design, MUI, Chakra) that bundle runtime styles or bloat the bundle.
  - **No Hand-Rolling Complex Widgets:** Do NOT reinvent the wheel or hand-code complex interactive UI patterns (e.g., date/range pickers, comboboxes/autocomplete, multi-select tags, rich sliders) from scratch with brittle custom state machines and bespoke keyboard logic.
  - **The Sweet Spot:** Use focused, lightweight, headless or unstyled single-purpose React packages (e.g., `react-day-picker`, Radix primitives, `floating-ui`, `cmdk`) and style them directly with Tailwind utility classes.
- **Mobile Touch Targets:** Interactive controls (buttons, inputs, selectables) must provide a minimum tap target height of 44px (`min-h-[44px]`).
- **Semantic HTML & A11y:** Use semantic elements (`<header>`, `<main>`, `<section>`, `<article>`, `<button type="button">`, `<input type="search">`). All icon-only interactive controls must declare an explicit `aria-label`.
- **Native `<dialog>` Management:** Trigger modals using imperative `.showModal()` on mount and handle dismissal via native events/backdrop clicks. Never pass `open={isOpen}` as a JSX attribute on `<dialog>`.

## Testing Standards
- **Test Structure (Pattern B):** All unit and component tests must be placed in dedicated `__tests__/` subdirectories within their corresponding domain or component folders (e.g., `src/components/events/__tests__/EventRow.test.tsx`).
- **Test Naming:** All test cases in Vitest must strictly follow the naming pattern of `Should_...`.
- **User-Centric Queries & Realistic Events:** Always query the DOM via Testing Library user-facing roles (`getByRole`, `getByLabelText`) and simulate user actions with `@testing-library/user-event` rather than `fireEvent`.
- **Contract Mocks via MSW:** Test data fetching hooks and API client functions against MSW network handlers. Do not manually mock `fetch` using `vi.fn()`.

## Comment Policy (Why, Never What)
- Write clean, self-documenting code with expressive naming so comments are rarely needed. Strictly forbid tautological comments (e.g., `// render row`, `// call api`, `// set state`). Comments are only permitted to explain the "why"—such as workarounds for third-party browser quirks, non-obvious date formatting edge cases, or upstream vendor payload anomalies. Delete boilerplate comments immediately.
