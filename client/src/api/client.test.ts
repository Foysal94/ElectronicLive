import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest'
import { fetchEvents } from './client'
import { ApiError } from './errors'
import type { EventResponse } from './types'

const sampleEvents: EventResponse[] = [
  {
    id: 'ra-101',
    name: 'Amelie Lens Exhale',
    venueName: 'Drumsheds',
    date: '2026-11-20',
    time: '21:00:00',
    ticketUrl: 'https://ra.co/events/101',
    status: 'OnSale',
    provider: 'ResidentAdvisor',
    offers: [
      {
        provider: 'ResidentAdvisor',
        ticketUrl: 'https://ra.co/events/101',
        status: 'OnSale',
      },
    ],
  },
]

const server = setupServer(
  http.get('/api/events/search', ({ request }) => {
    const url = new URL(request.url)
    const query = url.searchParams.get('query')

    if (query === 'error-500') {
      return HttpResponse.json(
        { title: 'Server Error', detail: 'Internal failure occurred.', status: 500 },
        { status: 500 }
      )
    }

    if (query === 'error-502') {
      return HttpResponse.json(
        {
          title: 'Upstream Providers Unavailable',
          detail: 'All external event providers failed to respond.',
          status: 502,
        },
        { status: 502 }
      )
    }

    if (query === 'invalid-schema') {
      return HttpResponse.json({ unexpected: 'payload' })
    }

    if (!query) {
      return HttpResponse.json(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { query: ['Search query parameter is required.'] },
        },
        { status: 400 }
      )
    }

    return HttpResponse.json(sampleEvents)
  })
)

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

describe('fetchEvents Client', () => {
  it('Should_fetch_events_successfully_for_valid_query', async () => {
    const results = await fetchEvents('Amelie Lens')

    expect(results).toHaveLength(1)
    expect(results[0]?.name).toBe('Amelie Lens Exhale')
    expect(results[0]?.provider).toBe('ResidentAdvisor')
  })

  it('Should_return_empty_array_without_request_when_query_is_whitespace', async () => {
    const results = await fetchEvents('   ')

    expect(results).toEqual([])
  })

  it('Should_throw_ApiError_with_details_when_server_returns_502_bad_gateway', async () => {
    const errorPromise = fetchEvents('error-502')

    await expect(errorPromise).rejects.toThrow(ApiError)
    await expect(errorPromise).rejects.toMatchObject({
      status: 502,
      title: 'Upstream Providers Unavailable',
      detail: 'All external event providers failed to respond.',
    })
  })

  it('Should_throw_ApiError_when_server_returns_500_error', async () => {
    const errorPromise = fetchEvents('error-500')

    await expect(errorPromise).rejects.toThrow(ApiError)
    await expect(errorPromise).rejects.toMatchObject({
      status: 500,
      title: 'Server Error',
      detail: 'Internal failure occurred.',
    })
  })

  it('Should_throw_ApiError_when_server_returns_invalid_schema', async () => {
    const errorPromise = fetchEvents('invalid-schema')

    await expect(errorPromise).rejects.toThrow(ApiError)
    await expect(errorPromise).rejects.toMatchObject({
      title: 'Invalid Schema',
    })
  })

  it('Should_pass_custom_city_parameter_in_url', async () => {
    let capturedUrl = ''
    server.use(
      http.get('/api/events/search', ({ request }) => {
        capturedUrl = request.url
        return HttpResponse.json(sampleEvents)
      })
    )

    await fetchEvents('Bicep', 'Manchester')

    const url = new URL(capturedUrl)
    expect(url.searchParams.get('query')).toBe('Bicep')
    expect(url.searchParams.get('city')).toBe('Manchester')
  })
})
