import { http, HttpResponse } from 'msw'
import { mockDefaultEvents } from './fixtures'

export const handlers = [
  http.get('*/api/events/search', ({ request }) => {
    const url = new URL(request.url)
    const query = url.searchParams.get('query')

    if (!query || !query.trim()) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { query: ['Search query parameter is required.'] },
        },
        { status: 400 }
      )
    }

    const trimmed = query.trim().toLowerCase()

    if (trimmed === 'error' || trimmed === 'error-500') {
      return HttpResponse.json(
        {
          title: 'Server Error',
          detail: 'Internal failure occurred while querying events.',
          status: 500,
        },
        { status: 500 }
      )
    }

    if (trimmed === 'error-502') {
      return HttpResponse.json(
        {
          title: 'Upstream Providers Unavailable',
          detail: 'All external event providers failed to respond.',
          status: 502,
        },
        { status: 502 }
      )
    }

    if (trimmed === 'invalid-schema') {
      return HttpResponse.json({ unexpected: 'malformed_payload' })
    }

    if (trimmed === 'all' || trimmed === '*') {
      return HttpResponse.json(mockDefaultEvents)
    }

    const matchingEvents = mockDefaultEvents.filter(
      (ev) =>
        ev.name.toLowerCase().includes(trimmed) ||
        ev.venueName.toLowerCase().includes(trimmed)
    )

    return HttpResponse.json(matchingEvents)
  }),
]
