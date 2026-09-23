import { ApiError, extractProblemDetails } from './errors'
import { type EventResponse, type EventSearchParams, isEventResponseList } from './types'

const API_BASE_URL =
  (import.meta.env.VITE_ELECTRONICLIVE_API_URL as string | undefined)?.replace(/\/+$/, '') || ''

/**
 * Dispatches an event aggregation search request to the backend API.
 * Accepts either a free-text search string (artist/venue) or an EventSearchParams object
 * containing free-text query, genre, and city.
 */
export async function fetchEvents(
  searchOrQuery: EventSearchParams | string,
  city = 'London',
  signal?: AbortSignal
): Promise<EventResponse[]> {
  const searchParams: EventSearchParams =
    typeof searchOrQuery === 'string'
      ? { query: searchOrQuery, city }
      : searchOrQuery

  const query = searchParams.query?.trim()
  const genre = searchParams.genre?.trim()
  const targetCity = (searchParams.city ?? city).trim()

  if (!query && !genre) {
    return []
  }

  const urlParams = new URLSearchParams()
  if (query) {
    urlParams.set('query', query)
  }
  if (genre) {
    urlParams.set('genre', genre)
  }
  if (targetCity) {
    urlParams.set('city', targetCity)
  }

  const endpoint = `${API_BASE_URL}/api/events/search?${urlParams.toString()}`
  const response = await fetch(endpoint, {
    headers: {
      Accept: 'application/json',
    },
    signal,
  })

  if (!response.ok) {
    const json: unknown = await response.json().catch(() => null)
    throw new ApiError(response.status, extractProblemDetails(json))
  }

  const data: unknown = await response.json()
  if (!isEventResponseList(data)) {
    throw new ApiError(response.status, {
      title: 'Invalid Schema',
      detail: 'The server response did not match the expected EventResponse contract.',
    })
  }

  return data
}
