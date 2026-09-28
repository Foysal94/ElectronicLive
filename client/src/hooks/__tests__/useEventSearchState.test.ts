import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { useEventSearchState } from '../useEventSearchState'

describe('useEventSearchState Hook', () => {
  beforeEach(() => {
    window.history.replaceState(null, '', '/')
  })

  afterEach(() => {
    window.history.replaceState(null, '', '/')
  })

  it('Should_initialize_idle_state_when_no_url_params_exist', () => {
    const { result } = renderHook(() => useEventSearchState())

    expect(result.current.searchTerm).toBe('')
    expect(result.current.activeQuery).toBe('')
    expect(result.current.activeGenre).toBe('')
  })

  it('Should_initialize_from_url_genre_parameter', () => {
    window.history.replaceState(null, '', '/?genre=techno')
    const { result } = renderHook(() => useEventSearchState())

    expect(result.current.activeGenre).toBe('techno')
    expect(result.current.activeQuery).toBe('')
    expect(result.current.searchTerm).toBe('')
  })

  it('Should_initialize_from_url_query_parameter', () => {
    window.history.replaceState(null, '', '/?q=Bicep')
    const { result } = renderHook(() => useEventSearchState())

    expect(result.current.activeQuery).toBe('Bicep')
    expect(result.current.searchTerm).toBe('Bicep')
    expect(result.current.activeGenre).toBe('')
  })

  it('Should_update_search_term_when_typing', () => {
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.setSearchTerm('Fabric')
    })

    expect(result.current.searchTerm).toBe('Fabric')
    // Typing alone should not change active search before submission
    expect(result.current.activeQuery).toBe('')
  })

  it('Should_execute_search_and_update_url_and_state', () => {
    window.history.replaceState(null, '', '/?genre=techno')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.handleSearch('Amelie Lens')
    })

    expect(result.current.activeQuery).toBe('Amelie Lens')
    expect(result.current.searchTerm).toBe('Amelie Lens')
    expect(result.current.activeGenre).toBe('')
    expect(window.location.search).toBe('?q=Amelie%20Lens')
  })

  it('Should_select_genre_clear_search_term_and_update_url', () => {
    window.history.replaceState(null, '', '/?q=Bicep')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.handleSelectGenre('drum-and-bass')
    })

    expect(result.current.activeGenre).toBe('drum-and-bass')
    expect(result.current.activeQuery).toBe('')
    expect(result.current.searchTerm).toBe('')
    expect(window.location.search).toBe('?genre=drum-and-bass')
  })

  it('Should_clear_all_filters_and_reset_url', () => {
    window.history.replaceState(null, '', '/?genre=trance')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.handleClear()
    })

    expect(result.current.activeGenre).toBe('')
    expect(result.current.activeQuery).toBe('')
    expect(result.current.searchTerm).toBe('')
    expect(window.location.search).toBe('')
  })

  it('Should_sync_state_and_input_on_browser_popstate', () => {
    window.history.replaceState(null, '', '/?q=Initial')
    const { result } = renderHook(() => useEventSearchState())

    expect(result.current.activeQuery).toBe('Initial')
    expect(result.current.searchTerm).toBe('Initial')

    act(() => {
      window.history.replaceState(null, '', '/?genre=house')
      window.dispatchEvent(new PopStateEvent('popstate'))
    })

    expect(result.current.activeGenre).toBe('house')
    expect(result.current.activeQuery).toBe('')
    expect(result.current.searchTerm).toBe('')
  })
})
