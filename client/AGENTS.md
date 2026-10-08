# Frontend Directives: ElectronicLive React Client

## Commands
- Dev Server: `npm run dev` (inside `/client`)
- Build: `npm run build`
- Run Tests: `npm run test`
- Lint & Typecheck: `npm run lint && npm run typecheck`

## Architecture & Conventions
- **API Client Layer:** All HTTP calls route through `src/api/client.ts` to the local .NET backend. Never call external third-party APIs directly from the browser.
- **Server State:** Use TanStack Query v5 object syntax in `src/hooks/`. Gate loading skeletons on `isFetching` (active network activity).
- **Component Layout:** Feature domains live under `src/components/<domain>/`, shared primitives under `src/components/common/`, layout under `src/components/layout/`.
- **React 19 & TypeScript:** Use standard function components (no `React.FC`). Pass `ref` as a normal prop (no `forwardRef`). Compute derived state during render; avoid syncing state in `useEffect`.
- **Date Handling:** Event dates (`YYYY-MM-DD`) must be parsed using UTC getters or string splitting to prevent timezone shifts.
- **Styling & Components:** Tailwind CSS for all styling. Use headless primitives (Radix, `react-datepicker`) for complex widgets; do not import monolithic UI frameworks (MUI, AntD). Mobile interactive controls require `min-h-[44px]`.

## Testing Standards
- Place component and hook tests in adjacent `__tests__/` subdirectories.
- Test Naming: Every test case must follow the naming pattern `Should_...`.
- Query DOM via user-facing roles (`getByRole`, `getByLabelText`) with `@testing-library/user-event`.
- Mock network interactions using MSW handlers; do not mock `fetch` via `vi.fn()`.
