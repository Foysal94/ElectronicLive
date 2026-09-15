import { describe, expect, it } from 'vitest'
import { ApiError, extractProblemDetails } from '../errors'

describe('ApiError and Problem Details Extraction', () => {
  it('Should_instantiate_ApiError_with_default_message_from_status', () => {
    const error = new ApiError(500)

    expect(error.name).toBe('ApiError')
    expect(error.status).toBe(500)
    expect(error.message).toBe('Request failed with status 500')
    expect(error).toBeInstanceOf(Error)
  })

  it('Should_instantiate_ApiError_with_detail_as_message_when_available', () => {
    const error = new ApiError(400, {
      title: 'Validation Error',
      detail: 'Query parameter is required.',
      errors: { query: ['Cannot be empty'] },
    })

    expect(error.status).toBe(400)
    expect(error.title).toBe('Validation Error')
    expect(error.detail).toBe('Query parameter is required.')
    expect(error.message).toBe('Query parameter is required.')
    expect(error.errors).toEqual({ query: ['Cannot be empty'] })
  })

  it('Should_extract_problem_details_from_rfc_payload', () => {
    const payload = {
      title: 'Bad Gateway',
      detail: 'Upstream providers failed.',
      status: 502,
      errors: {
        provider: ['ResidentAdvisor timed out'],
      },
    }

    const result = extractProblemDetails(payload)

    expect(result.title).toBe('Bad Gateway')
    expect(result.detail).toBe('Upstream providers failed.')
    expect(result.status).toBe(502)
    expect(result.errors).toEqual({ provider: ['ResidentAdvisor timed out'] })
  })

  it('Should_handle_non_object_or_null_payload_gracefully', () => {
    expect(extractProblemDetails(null)).toEqual({})
    expect(extractProblemDetails('not json')).toEqual({})
    expect(extractProblemDetails(123)).toEqual({})
  })
})
