import { useQuery } from '@tanstack/react-query'
import { fetchEvents } from '../api/client'
import type { EventResponse } from '../api/types'

export interface UseEventsSearchResult {
  events: EventResponse[]
  isPending: boolean
  isFetching: boolean
  isError: boolean
  error: Error | null
  refetch: () => Promise<unknown>
  isIdle: boolean
}

export function useEventsSearch(query: string, city = 'London'): UseEventsSearchResult {
  const trimmedQuery = query.trim()
  const trimmedCity = city.trim()
  const isIdle = !trimmedQuery

  const { data, isPending, isFetching, isError, error, refetch } = useQuery({
    queryKey: ['events', 'search', trimmedQuery.toLowerCase(), trimmedCity.toLowerCase()],
    queryFn: ({ signal }) => fetchEvents(trimmedQuery, trimmedCity, signal),
    enabled: !isIdle,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })

  return {
    events: data ?? [],
    isPending,
    isFetching,
    isError,
    error: error ?? null,
    refetch,
    isIdle,
  }
}
