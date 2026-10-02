import { differenceInCalendarDays, parseISO } from 'date-fns'
import { http, HttpResponse } from 'msw'
import { isValidEmail } from '../../utils/validation'
import { mockDefaultEvents } from './fixtures'

export const handlers = [
  http.get('*/api/events/search', ({ request }) => {
    const url = new URL(request.url)
    const query = url.searchParams.get('query')?.trim() || ''
    const genre = url.searchParams.get('genre')?.trim().toLowerCase() || ''
    const from = url.searchParams.get('from')?.trim() || ''
    const to = url.searchParams.get('to')?.trim() || ''

    if (!query && !genre && !from && !to) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { query: ['At least one of query, genre, or date parameters must be provided.'] },
        },
        { status: 400 }
      )
    }

    if (!query && !genre && (!from || !to)) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { date: ["Both 'from' and 'to' date parameters are required for date-only queries."] },
        },
        { status: 400 }
      )
    }

    if (from && to && from > to) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { to: ["'to' date must be greater than or equal to 'from' date."] },
        },
        { status: 400 }
      )
    }

    if (!query && !genre && from && to) {
      const diffDays = differenceInCalendarDays(parseISO(to), parseISO(from))
      if (diffDays > 7) {
        return HttpResponse.json(
          {
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: { date: ['Date range cannot exceed 7 days when searching without query or genre.'] },
          },
          { status: 400 }
        )
      }
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

    let results = mockDefaultEvents

    if (from || to) {
      results = results.filter((ev) => {
        if (!ev.date) return false
        if (from && ev.date < from) return false
        if (to && ev.date > to) return false
        return true
      })
    }

    if (genre) {
      results = results.filter((ev) => {
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
    }

    if (trimmed && trimmed !== 'all' && trimmed !== '*') {
      results = results.filter(
        (ev) =>
          ev.name.toLowerCase().includes(trimmed) ||
          ev.venueName.toLowerCase().includes(trimmed)
      )
    }

    return HttpResponse.json(results)
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

    if (!isValidEmail(emailStr)) {
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

