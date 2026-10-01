import { ApiError, extractProblemDetails } from './errors'
import {
  type CreateSubscriptionRequest,
  type EventResponse,
  type EventSearchParams,
  type SubscriptionResponse,
  isEventResponseList,
  isSubscriptionResponse,
} from './types'

const API_BASE_URL =
  (import.meta.env.VITE_ELECTRONICLIVE_API_URL as string | undefined)?.replace(/\/+$/, '') || ''

async function parseApiResponse<T>(
  response: Response,
  guard: (data: unknown) => data is T,
  schemaErrorMessage: string
): Promise<T> {
  if (!response.ok) {
    const json: unknown = await response.json().catch(() => null)
    throw new ApiError(response.status, extractProblemDetails(json))
  }

  const data: unknown = await response.json()
  if (!guard(data)) {
    throw new ApiError(response.status, {
      title: 'Invalid Schema',
      detail: schemaErrorMessage,
    })
  }

  return data
}

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
  const from = searchParams.from?.trim()
  const to = searchParams.to?.trim()
  const targetCity = (searchParams.city ?? city).trim()

  if (!query && !genre && !from && !to) {
    return []
  }

  const urlParams = new URLSearchParams()
  if (query) {
    urlParams.set('query', query)
  }
  if (genre) {
    urlParams.set('genre', genre)
  }
  if (from) {
    urlParams.set('from', from)
  }
  if (to) {
    urlParams.set('to', to)
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

  return parseApiResponse(
    response,
    isEventResponseList,
    'The server response did not match the expected EventResponse contract.'
  )
}

export async function createSubscription(
  payload: CreateSubscriptionRequest,
  signal?: AbortSignal
): Promise<SubscriptionResponse> {
  const endpoint = `${API_BASE_URL}/api/subscriptions`
  const response = await fetch(endpoint, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify(payload),
    signal,
  })

  return parseApiResponse(
    response,
    isSubscriptionResponse,
    'The server response did not match the expected SubscriptionResponse contract.'
  )
}

