import { describe, expect, it } from 'vitest'
import { fetchEvents } from '../../api/client'
import { ApiError } from '../../api/errors'

describe('MSW Handlers Integration', () => {
  it('Should_intercept_events_search_and_return_matching_fixtures', async () => {
    const events = await fetchEvents('Amelie')

    expect(events).toHaveLength(1)
    expect(events[0]?.name).toBe('Amelie Lens - Exhale London')
    expect(events[0]?.venueName).toBe('Drumsheds')
    expect(events[0]?.offers).toHaveLength(3)
  })

  it('Should_return_empty_array_when_query_matches_no_events', async () => {
    const events = await fetchEvents('empty')

    expect(events).toEqual([])
  })

  it('Should_return_502_problem_details_when_query_is_error_502', async () => {
    const promise = fetchEvents('error-502')

    await expect(promise).rejects.toThrow(ApiError)
    await expect(promise).rejects.toMatchObject({
      status: 502,
      title: 'Upstream Providers Unavailable',
    })
  })

  it('Should_return_500_server_error_when_query_is_error_500', async () => {
    const promise = fetchEvents('error-500')

    await expect(promise).rejects.toThrow(ApiError)
    await expect(promise).rejects.toMatchObject({
      status: 500,
      title: 'Server Error',
    })
  })

  it('Should_handle_invalid_schema_from_mock_server', async () => {
    const promise = fetchEvents('invalid-schema')

    await expect(promise).rejects.toThrow(ApiError)
    await expect(promise).rejects.toMatchObject({
      title: 'Invalid Schema',
    })
  })
})
