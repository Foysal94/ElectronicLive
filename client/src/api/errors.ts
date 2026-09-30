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
    const firstFieldError = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined
    super(problem?.detail || firstFieldError || problem?.title || `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.title = problem?.title
    this.detail = problem?.detail
    this.errors = problem?.errors
  }
}


export function extractProblemDetails(json: unknown): ApiProblemDetails {
  if (typeof json !== 'object' || json === null) {
    return {}
  }

  const title = 'title' in json && typeof json.title === 'string' ? json.title : undefined
  const detail = 'detail' in json && typeof json.detail === 'string' ? json.detail : undefined
  const status = 'status' in json && typeof json.status === 'number' ? json.status : undefined

  let errors: Record<string, string[]> | undefined
  if ('errors' in json && typeof json.errors === 'object' && json.errors !== null) {
    const errorEntries: Record<string, string[]> = {}
    for (const [key, value] of Object.entries(json.errors)) {
      if (Array.isArray(value) && value.every((entry) => typeof entry === 'string')) {
        errorEntries[key] = value
      }
    }
    errors = errorEntries
  }

  return { title, detail, status, errors }
}
