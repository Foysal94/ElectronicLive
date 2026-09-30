import { renderHook, waitFor, act } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/errors'
import { createQueryWrapper } from '../../test/utils'
import { useCreateSubscription } from '../useCreateSubscription'

describe('useCreateSubscription Hook', () => {
  it('Should_mutate_and_succeed_for_valid_subscription_request', async () => {
    const onSuccess = vi.fn()
    const { result } = renderHook(() => useCreateSubscription({ onSuccess }), {
      wrapper: createQueryWrapper(),
    })

    expect(result.current.isPending).toBe(false)
    expect(result.current.isSuccess).toBe(false)

    act(() => {
      result.current.mutate({
        email: 'user@example.com',
        artistName: 'Bicep',
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data).toEqual({
      subscriptionId: '123e4567-e89b-12d3-a456-426614174000',
      message: 'Subscribed successfully',
    })
    expect(onSuccess).toHaveBeenCalledWith(
      expect.objectContaining({
        subscriptionId: '123e4567-e89b-12d3-a456-426614174000',
        message: 'Subscribed successfully',
      })
    )
  })

  it('Should_handle_mutation_error_and_invoke_onError_callback', async () => {
    const onError = vi.fn()
    const { result } = renderHook(() => useCreateSubscription({ onError }), {
      wrapper: createQueryWrapper(),
    })

    act(() => {
      result.current.mutate({
        email: 'invalid-email',
        artistName: 'Bicep',
      })
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.error).toBeInstanceOf(ApiError)
    expect(onError).toHaveBeenCalledWith(expect.any(ApiError))
  })
})
