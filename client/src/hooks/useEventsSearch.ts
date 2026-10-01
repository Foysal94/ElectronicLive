import { useQuery } from '@tanstack/react-query'
import { fetchEvents } from '../api/client'
import type { EventResponse, EventSearchParams } from '../api/types'

export interface UseEventsSearchResult {
  events: EventResponse[]
  isPending: boolean
  isFetching: boolean
  isError: boolean
  error: Error | null
  refetch: () => Promise<unknown>
  isIdle: boolean
}

export function useEventsSearch(
  searchOrQuery: EventSearchParams | string,
  city = 'London'
): UseEventsSearchResult {
  const searchParams: EventSearchParams =
    typeof searchOrQuery === 'string'
      ? { query: searchOrQuery, city }
      : searchOrQuery

  const query = searchParams.query?.trim() ?? ''
  const genre = searchParams.genre
  const from = searchParams.from?.trim() || undefined
  const to = searchParams.to?.trim() || undefined
  const targetCity = (searchParams.city ?? city).trim() || 'London'
  // Date-only queries require both 'from' and 'to' parameters per backend contract.
  // Incomplete date bounds without query or genre remain idle to prevent 400 Bad Request errors.
  const hasTextOrGenre = Boolean(query || genre)
  const hasCompleteDateRange = Boolean(from && to)
  const isIdle = !hasTextOrGenre && !hasCompleteDateRange

  const { data, isPending, isFetching, isError, error, refetch } = useQuery({
    queryKey: [
      'events',
      'search',
      {
        query: query.toLowerCase(),
        genre: genre ?? null,
        city: targetCity.toLowerCase(),
        from: from ?? null,
        to: to ?? null,
      },
    ],
    queryFn: ({ signal }) =>
      fetchEvents({ query, genre, from, to, city: targetCity }, targetCity, signal),
    enabled: !isIdle,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })

  return {
    events: data ?? [],
    // In TanStack Query v5, disabled queries retain status: 'pending' when no cached data exists.
    // Explicitly negate isPending when idle to prevent views from flashing premature loading skeletons.
    isPending: !isIdle && isPending,
    isFetching,
    isError,
    error: error ?? null,
    refetch,
    isIdle,
  }
}
