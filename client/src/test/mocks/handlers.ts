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

  http.post('*/api/subscriptions', async ({ request }) => {
    let body: unknown
    try {
      body = await request.json()
    } catch {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { request: ['Malformed JSON body.'] },
        },
        { status: 400 }
      )
    }

    const { email, artistName, city = 'London' } = (body as Record<string, unknown>) || {}
    const emailStr = typeof email === 'string' ? email.trim() : ''
    const artistStr = typeof artistName === 'string' ? artistName.trim() : ''

    if (!emailStr || !emailStr.includes('@')) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { email: ['A valid email address is required.'] },
        },
        { status: 400 }
      )
    }

    if (!artistStr) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { artistName: ['Artist name is required.'] },
        },
        { status: 400 }
      )
    }

    if (artistStr.toLowerCase() === 'unknownartist' || artistStr.toLowerCase() === 'unverified') {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { artistName: [`Artist '${artistStr}' could not be verified as a genuine music entity.`] },
        },
        { status: 400 }
      )
    }

    if (emailStr === 'existing@example.com') {
      return HttpResponse.json(
        {
          subscriptionId: '123e4567-e89b-12d3-a456-426614174000',
          message: `Already subscribed to ${artistStr} in ${city || 'London'}.`,
        },
        { status: 200 }
      )
    }

    return HttpResponse.json(
      {
        subscriptionId: '123e4567-e89b-12d3-a456-426614174000',
        message: 'Subscribed successfully',
      },
      { status: 201 }
    )
  }),
]

