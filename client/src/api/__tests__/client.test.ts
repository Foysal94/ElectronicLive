import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '../../test/mocks/server'
import { createSubscription, fetchEvents } from '../client'
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

describe('createSubscription Client', () => {
  it('Should_create_subscription_successfully_for_valid_payload', async () => {
    const response = await createSubscription({
      email: 'user@example.com',
      artistName: 'Bicep',
    })

    expect(response.subscriptionId).toBe('123e4567-e89b-12d3-a456-426614174000')
    expect(response.message).toBe('Subscribed successfully')
  })

  it('Should_return_success_message_when_already_subscribed', async () => {
    const response = await createSubscription({
      email: 'existing@example.com',
      artistName: 'Bicep',
    })

    expect(response.subscriptionId).toBe('123e4567-e89b-12d3-a456-426614174000')
    expect(response.message).toContain('Already subscribed')
  })

  it('Should_throw_ApiError_when_validation_fails', async () => {
    const promise = createSubscription({
      email: 'invalid-email',
      artistName: 'Bicep',
    })

    await expect(promise).rejects.toThrow(ApiError)
    await expect(promise).rejects.toMatchObject({
      status: 400,
      errors: { email: ['A valid email address is required.'] },
    })
  })

  it('Should_throw_ApiError_when_artist_is_unverified', async () => {
    const promise = createSubscription({
      email: 'user@example.com',
      artistName: 'UnknownArtist',
    })

    await expect(promise).rejects.toThrow(ApiError)
    await expect(promise).rejects.toMatchObject({
      status: 400,
      errors: { artistName: ["Artist 'UnknownArtist' could not be verified as a genuine music entity."] },
    })
  })

  it('Should_throw_ApiError_when_server_returns_500', async () => {
    const promise = createSubscription({
      email: 'server-error@example.com',
      artistName: 'Bicep',
    })

    await expect(promise).rejects.toThrow(ApiError)
    await expect(promise).rejects.toMatchObject({
      status: 500,
      detail: 'Failed to create subscription.',
    })
  })

  it('Should_throw_ApiError_when_response_schema_is_invalid', async () => {
    const promise = createSubscription({
      email: 'invalid-schema@example.com',
      artistName: 'Bicep',
    })

    await expect(promise).rejects.toThrow(ApiError)
    await expect(promise).rejects.toMatchObject({
      status: 200,
      title: 'Invalid Schema',
    })
  })
})

