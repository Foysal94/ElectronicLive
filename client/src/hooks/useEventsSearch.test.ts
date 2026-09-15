import { renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ApiError } from '../api/errors'
import { createQueryWrapper } from '../test/utils'
import { useEventsSearch } from './useEventsSearch'

describe('useEventsSearch Hook', () => {
  it('Should_fetch_and_cache_events_for_valid_query', async () => {
    const { result } = renderHook(() => useEventsSearch('Amelie'), {
      wrapper: createQueryWrapper(),
    })

    expect(result.current.isIdle).toBe(false)

    await waitFor(() => {
      expect(result.current.isFetching).toBe(false)
    })

    expect(result.current.isError).toBe(false)
    expect(result.current.events.length).toBeGreaterThan(0)
    expect(result.current.events[0]?.name).toBe('Amelie Lens - Exhale London')
  })

  it('Should_remain_idle_and_not_fetch_when_query_is_empty', () => {
    const { result } = renderHook(() => useEventsSearch('   '), {
      wrapper: createQueryWrapper(),
    })

    expect(result.current.isIdle).toBe(true)
    expect(result.current.isPending).toBe(false)
    expect(result.current.isFetching).toBe(false)
    expect(result.current.events).toEqual([])
    expect(result.current.isError).toBe(false)
  })

  it('Should_handle_api_errors_gracefully', async () => {
    const { result } = renderHook(() => useEventsSearch('error-500'), {
      wrapper: createQueryWrapper(),
    })

    expect(result.current.isIdle).toBe(false)

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.isFetching).toBe(false)
    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(result.current.events).toEqual([])
  })
})
