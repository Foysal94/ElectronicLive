import { http, HttpResponse } from 'msw'
import { mockDefaultEvents } from './fixtures'

export const handlers = [
  http.get('*/api/events/search', ({ request }) => {
    const url = new URL(request.url)
    const query = url.searchParams.get('query')?.trim() || ''
    const genre = url.searchParams.get('genre')?.trim().toLowerCase() || ''

    if (!query && !genre) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { query: ['At least one of query or genre parameter must be provided.'] },
        },
        { status: 400 }
      )
    }

    const trimmed = query.toLowerCase()

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

    if (genre) {
      let genreEvents = mockDefaultEvents.filter((ev) => {
        const evName = ev.name.toLowerCase()
        if (genre === 'techno') {
          return evName.includes('techno') || evName.includes('amelie') || evName.includes('charlotte')
        }
        if (genre === 'house') {
          return evName.includes('house') || evName.includes('defected')
        }
        if (genre === 'drum-and-bass') {
          return evName.includes('drum & bass') || evName.includes('hospitality')
        }
        if (genre === 'trance') {
          return evName.includes('hardwell') || evName.includes('armin')
        }
        return false
      })

      if (query) {
        genreEvents = genreEvents.filter(
          (ev) =>
            ev.name.toLowerCase().includes(trimmed) ||
            ev.venueName.toLowerCase().includes(trimmed)
        )
      }

      return HttpResponse.json(genreEvents)
    }

    const matchingEvents = mockDefaultEvents.filter(
      (ev) =>
        ev.name.toLowerCase().includes(trimmed) ||
        ev.venueName.toLowerCase().includes(trimmed)
    )

    return HttpResponse.json(matchingEvents)
  }),
]
