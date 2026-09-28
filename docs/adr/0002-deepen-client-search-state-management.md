# ADR 0002: Deepen Client Search State Management

## Status
Accepted

## Context
Previously, client-side search state was fragmented across multiple files and hooks:
1. `useAppSearchParams` in `useSearchParam.ts` synchronized browser URL query parameters (`?q=` / `?genre=`) with mutual exclusivity.
2. `App.tsx` maintained local input text state (`searchTerm`) and two previous-state trackers (`prevActiveQuery`, `prevActiveGenre`), performing render-phase state adjustments (`if (prevActiveQuery !== activeQuery ...)`) to keep the search bar synchronized with URL changes.
3. `useSearchParam.ts` exported an unused generic helper (`useSearchParam`) representing dead code.

This pattern forced `App.tsx` to act as a state reconciliation manager rather than a clean presentational component.

## Decision
1. Consolidate search text input state, browser URL synchronization, and filter mutual exclusivity into a single dedicated custom hook: `useEventSearchState` in `client/src/hooks/useEventSearchState.ts`.
2. Maintain clean separation between UI search state and server-data fetching: `useEventSearchState` remains state-only with zero network dependencies, while `useEventsSearch` continues to handle TanStack Query data fetching.
3. Delete the dead generic helper `useSearchParam` and replace `useSearchParam.ts` and its test suite with `useEventSearchState.ts` and `useEventSearchState.test.ts`.
4. Use `window.history.replaceState` for clean in-place address bar synchronization and listen to window `popstate` events to handle browser Back/Forward navigation.
5. Auto-synchronize `searchTerm` on filter changes: selecting a genre clears the input text, while text searches and URL query navigation populate it.
6. Refactor `App.tsx` to consume `useEventSearchState` directly, eliminating all render-phase sync state variables.

## Consequences
- **Locality**: Search input state, genre exclusivity, and URL bar synchronization are co-located in a single lightweight (~50 line) hook.
- **Pure View**: `App.tsx` is stripped of state-reconciliation plumbing and becomes a deterministic view layer.
- **Dead Code Removed**: Unused `useSearchParam` helper is removed.
- **Focused Testing**: `useEventSearchState` can be tested via `renderHook` as pure state with zero network mocking, while `App.tsx` integration tests remain intact.
