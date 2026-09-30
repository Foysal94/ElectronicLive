import { useMutation, type UseMutationResult } from '@tanstack/react-query'
import { createSubscription } from '../api/client'
import type { CreateSubscriptionRequest, SubscriptionResponse } from '../api/types'
import { useToast } from './useToast'


export interface UseCreateSubscriptionOptions {
  onSuccess?: (data: SubscriptionResponse) => void
  onError?: (error: Error) => void
}

export function useCreateSubscription(
  options: UseCreateSubscriptionOptions = {}
): UseMutationResult<SubscriptionResponse, Error, CreateSubscriptionRequest> {
  const { showToast } = useToast()

  return useMutation({
    mutationFn: (payload: CreateSubscriptionRequest) => createSubscription(payload),
    onSuccess: (data) => {
      showToast(data.message, 'success')
      options.onSuccess?.(data)
    },
    onError: (error) => {
      options.onError?.(error)
    },
  })
}

