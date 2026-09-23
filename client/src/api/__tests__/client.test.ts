import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '../../test/mocks/server'
import { fetchEvents } from '../client'
import { ApiError } from '../errors'

describe('fetchEvents Client', () => {
  it('Should_fetch_events_successfully_for_valid_query', async () => {
    const results = await fetchEvents('Amelie')

    expect(results.length).toBeGreaterThan(0)
    expect(results[0]?.name).toBe('Amelie Lens - Exhale London')
    expect(results[0]?.provider).toBe('ResidentAdvisor')
  })

  it('Should_fetch_events_successfully_for_genre_parameter', async () => {
    const results = await fetchEvents({ genre: 'techno' })

    expect(results.length).toBeGreaterThan(0)
    expect(results.some((ev) => ev.name.includes('Techno'))).toBe(true)
  })

  it('Should_fetch_events_successfully_when_both_query_and_genre_provided', async () => {
    const results = await fetchEvents({ query: 'Charlotte', genre: 'techno' })

    expect(results.length).toBeGreaterThan(0)
    expect(results[0]?.name).toContain('Charlotte')
  })

  it('Should_return_empty_array_without_request_when_query_is_whitespace', async () => {
    const results = await fetchEvents('   ')

    expect(results).toEqual([])
  })

  it('Should_return_empty_array_without_request_when_both_query_and_genre_are_empty', async () => {
    const results = await fetchEvents({ query: '   ', genre: undefined })

    expect(results).toEqual([])
  })

  it('Should_return_empty_array_when_query_yields_no_keyword_matches', async () => {
    const results = await fetchEvents('NonexistentArtist')

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
      detail: 'Internal failure occurred while querying events.',
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
      http.get('*/api/events/search', ({ request }) => {
        capturedUrl = request.url
        return HttpResponse.json([])
      })
    )

    await fetchEvents('Bicep', 'Manchester')

    const url = new URL(capturedUrl)
    expect(url.searchParams.get('query')).toBe('Bicep')
    expect(url.searchParams.get('city')).toBe('Manchester')
  })
})
