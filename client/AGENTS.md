# Frontend Directives: ElectronicLive React Client

## Tech Stack
- React 19, TypeScript (Strict Mode)
- Query/State: TanStack Query (React Query)
- Styling: Tailwind CSS
- Testing: Vitest, React Testing Library, MSW

## Commands
- Dev Server: `npm run dev` (run inside `/client`)
- Build: `npm run build`
- Run Tests: `npm run test`
- Lint & Typecheck: `npm run lint && npm run typecheck`

## Architecture & Code Boundaries
- **Single Target**: All fetch requests target the local .NET Web API. Never make third-party vendor requests directly from the client.
- **Data Fetching**: Wrap all API calls in typed custom hooks inside `src/api/` or `src/features/` using TanStack Query.
- **Resilience UI**: Since the backend aggregates multi-source gig data, views must cleanly render:
  1. Loading skeleton states
  2. Partial or empty data states
  3. Network error boundaries

## Guardrails
- DO NOT use the `any` type.
- DO NOT use raw `fetch()` or `axios` inside `useEffect`.
