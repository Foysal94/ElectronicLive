import { type EventResponse, isEventResponseList } from './types'

export interface ApiProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly title?: string
  readonly detail?: string
  readonly errors?: Record<string, string[]>

  constructor(status: number, problem?: ApiProblemDetails) {
    super(problem?.detail || problem?.title || `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.title = problem?.title
    this.detail = problem?.detail
    this.errors = problem?.errors
  }
}

function parseProblemDetails(json: unknown): ApiProblemDetails {
  if (typeof json !== 'object' || json === null) {
    return {}
  }

  const problem: ApiProblemDetails = {}
  if ('title' in json && typeof json.title === 'string') {
    problem.title = json.title
  }
  if ('detail' in json && typeof json.detail === 'string') {
    problem.detail = json.detail
  }
  if ('status' in json && typeof json.status === 'number') {
    problem.status = json.status
  }
  if ('errors' in json && typeof json.errors === 'object' && json.errors !== null) {
    const errorEntries: Record<string, string[]> = {}
    for (const [key, value] of Object.entries(json.errors)) {
      if (Array.isArray(value) && value.every((entry) => typeof entry === 'string')) {
        errorEntries[key] = value
      }
    }
    problem.errors = errorEntries
  }

  return problem
}

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
    let problem: ApiProblemDetails | undefined
    try {
      const json: unknown = await response.json()
      problem = parseProblemDetails(json)
    } catch {
      // Body is not JSON
    }
    throw new ApiError(response.status, problem)
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
