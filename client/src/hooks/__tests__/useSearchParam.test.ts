import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { useAppSearchParams, useSearchParam } from '../useSearchParam'


describe('useSearchParam Hook', () => {
  beforeEach(() => {
    window.history.replaceState(null, '', '/')
  })

  afterEach(() => {
    window.history.replaceState(null, '', '/')
  })

  it('Should_initialize_from_url_parameter', () => {
    window.history.replaceState(null, '', '/?q=Bicep')

    const { result } = renderHook(() => useSearchParam('q'))

    expect(result.current[0]).toBe('Bicep')
  })

  it('Should_update_url_and_state_when_setParam_is_called', () => {
    const { result } = renderHook(() => useSearchParam('q'))

    act(() => {
      result.current[1]('Amelie Lens')
    })

    expect(result.current[0]).toBe('Amelie Lens')
    expect(window.location.search).toBe('?q=Amelie%20Lens')
  })

  it('Should_clear_url_parameter_when_empty_string_passed', () => {
    window.history.replaceState(null, '', '/?q=Fabric')
    const { result } = renderHook(() => useSearchParam('q'))

    act(() => {
      result.current[1]('')
    })

    expect(result.current[0]).toBe('')
    expect(window.location.search).toBe('')
  })

  it('Should_update_state_on_popstate_event', () => {
    window.history.replaceState(null, '', '/?q=Initial')
    const { result } = renderHook(() => useSearchParam('q'))

    expect(result.current[0]).toBe('Initial')

    act(() => {
      window.history.replaceState(null, '', '/?q=Updated')
      window.dispatchEvent(new PopStateEvent('popstate'))
    })

    expect(result.current[0]).toBe('Updated')
  })
})

describe('useAppSearchParams Hook', () => {
  beforeEach(() => {
    window.history.replaceState(null, '', '/')
  })

  afterEach(() => {
    window.history.replaceState(null, '', '/')
  })

  it('Should_initialize_query_and_genre_from_url', () => {
    window.history.replaceState(null, '', '/?genre=techno')
    const { result } = renderHook(() => useAppSearchParams())

    expect(result.current.genre).toBe('techno')
    expect(result.current.query).toBe('')
  })

  it('Should_set_query_and_clear_genre_in_url', () => {
    window.history.replaceState(null, '', '/?genre=techno')
    const { result } = renderHook(() => useAppSearchParams())

    act(() => {
      result.current.setQuery('Bicep')
    })

    expect(result.current.query).toBe('Bicep')
    expect(result.current.genre).toBe('')
    expect(window.location.search).toBe('?q=Bicep')
  })

  it('Should_set_genre_and_clear_query_in_url', () => {
    window.history.replaceState(null, '', '/?q=Bicep')
    const { result } = renderHook(() => useAppSearchParams())

    act(() => {
      result.current.setGenre('drum-and-bass')
    })

    expect(result.current.genre).toBe('drum-and-bass')
    expect(result.current.query).toBe('')
    expect(window.location.search).toBe('?genre=drum-and-bass')
  })

  it('Should_clear_all_parameters_in_url', () => {
    window.history.replaceState(null, '', '/?genre=trance')
    const { result } = renderHook(() => useAppSearchParams())

    act(() => {
      result.current.clearAll()
    })

    expect(result.current.genre).toBe('')
    expect(result.current.query).toBe('')
    expect(window.location.search).toBe('')
  })
})
