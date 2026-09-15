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
- **Single Target:** All client fetch requests must target the local .NET Web API. Never query external third-party vendor APIs directly from the browser.
- **Dedicated Data Hooks:** Encapsulate all API queries in custom hooks under `src/hooks/` using TanStack Query v5 object syntax (`useQuery({ queryKey, queryFn })`). Never place raw fetch calls or `useQuery` invocations directly inside UI components.
- **Domain-Organized Components:** Organize UI components by feature domain under `components/<domain>/`, with shared primitives under `components/common/` and global chrome under `components/layout/`.
- **Pure Leaf Components:** Presentational components must remain pure, deterministic functions (props in -> JSX out). Keep server state, caching, and query orchestration isolated in parent views or custom hooks.
- **Resilience UI:** Views consuming asynchronous data must cleanly render:
  1. Loading skeleton states
  2. Partial or empty data states
  3. Network error boundaries with user-friendly retry actions

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
- **Test Naming:** All test cases in Vitest must strictly follow the naming pattern of `Should_...` (e.g., `it('Should_render_results_when_query_succeeds', ...)`).
- **Tooling:** Use Vitest + React Testing Library + `@testing-library/user-event`. Enzyme is strictly forbidden.
- **User-Centric Queries:** Always query the DOM via Testing Library user-facing roles (`getByRole`, `getByLabelText`). Never query by CSS class names, element IDs, or DOM hierarchy.
- **Realistic Events:** Use `@testing-library/user-event` rather than `fireEvent` to simulate realistic browser interactions.
- **Contract Mocks via MSW:** Test data fetching hooks against MSW network handlers. Do not manually mock `fetch` using `vi.fn()`.

## Guardrails
- DO NOT add unapproved third-party UI component libraries (e.g., MUI, AntD, Chakra); build clean primitives with Tailwind CSS.
- DO NOT use raw `fetch()` or `axios` inside `useEffect`.

## Comment Policy (Why, Never What)
- Write clean, self-documenting code with expressive naming so comments are rarely needed. Strictly forbid tautological comments (e.g., `// render row`, `// call api`, `// set state`). Comments are only permitted to explain the "why"—such as workarounds for third-party browser quirks (e.g., popup blocker behavior on external ticket links), non-obvious date formatting edge cases, or upstream vendor payload anomalies. Delete boilerplate comments immediately.
