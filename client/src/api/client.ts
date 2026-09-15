import { ApiError, extractProblemDetails } from './errors'
import { type EventResponse, isEventResponseList } from './types'

export async function fetchEvents(
  query: string,
  city = 'London',
  signal?: AbortSignal
): Promise<EventResponse[]> {
  const trimmedQuery = query.trim()
  if (!trimmedQuery) {
    return []
  }

  const params = new URLSearchParams({ query: trimmedQuery })
  const trimmedCity = city.trim()
  if (trimmedCity) {
    params.set('city', trimmedCity)
  }

  const response = await fetch(`/api/events/search?${params.toString()}`, {
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
