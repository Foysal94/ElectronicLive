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

  it('Should_extract_initial_from_and_to_parameters_from_url', () => {
    window.history.replaceState(null, '', '/?from=2026-10-10&to=2026-10-12')
    const { result } = renderHook(() => useEventSearchState())

    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')
    expect(result.current.activeQuery).toBe('')
    expect(result.current.activeGenre).toBe('')
  })

  it('Should_extract_both_query_and_date_parameters_from_url', () => {
    window.history.replaceState(null, '', '/?q=Bicep&from=2026-10-10&to=2026-10-12')
    const { result } = renderHook(() => useEventSearchState())

    expect(result.current.activeQuery).toBe('Bicep')
    expect(result.current.searchTerm).toBe('Bicep')
    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')
  })

  it('Should_update_date_range_and_sync_with_url', () => {
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.setDateRange('2026-11-01', '2026-11-03')
    })

    expect(result.current.activeFrom).toBe('2026-11-01')
    expect(result.current.activeTo).toBe('2026-11-03')
    expect(window.location.search).toBe('?from=2026-11-01&to=2026-11-03')
  })

  it('Should_combine_date_range_orthogonally_with_active_query', () => {
    window.history.replaceState(null, '', '/?q=Bicep')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.setDateRange('2026-10-10', '2026-10-12')
    })

    expect(result.current.activeQuery).toBe('Bicep')
    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')
    expect(window.location.search).toBe('?q=Bicep&from=2026-10-10&to=2026-10-12')
  })

  it('Should_combine_date_range_orthogonally_with_active_genre', () => {
    window.history.replaceState(null, '', '/?genre=techno')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.setDateRange('2026-10-10', '2026-10-12')
    })

    expect(result.current.activeGenre).toBe('techno')
    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')
    expect(window.location.search).toBe('?genre=techno&from=2026-10-10&to=2026-10-12')
  })

  it('Should_preserve_date_range_when_searching_new_query', () => {
    window.history.replaceState(null, '', '/?from=2026-10-10&to=2026-10-12')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.handleSearch('Charlotte')
    })

    expect(result.current.activeQuery).toBe('Charlotte')
    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')
    expect(window.location.search).toBe('?q=Charlotte&from=2026-10-10&to=2026-10-12')
  })

  it('Should_preserve_date_range_when_selecting_genre', () => {
    window.history.replaceState(null, '', '/?from=2026-10-10&to=2026-10-12')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.handleSelectGenre('house')
    })

    expect(result.current.activeGenre).toBe('house')
    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')
    expect(window.location.search).toBe('?genre=house&from=2026-10-10&to=2026-10-12')
  })

  it('Should_preserve_date_range_when_clearing_query_via_handleClear', () => {
    window.history.replaceState(null, '', '/?q=Bicep&from=2026-10-10&to=2026-10-12')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.handleClear()
    })

    expect(result.current.activeQuery).toBe('')
    expect(result.current.searchTerm).toBe('')
    expect(result.current.activeGenre).toBe('')
    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')
    expect(window.location.search).toBe('?from=2026-10-10&to=2026-10-12')
  })

  it('Should_clear_date_range_when_calling_setDateRange_with_undefined', () => {
    window.history.replaceState(null, '', '/?q=Bicep&from=2026-10-10&to=2026-10-12')
    const { result } = renderHook(() => useEventSearchState())

    act(() => {
      result.current.setDateRange(undefined, undefined)
    })

    expect(result.current.activeQuery).toBe('Bicep')
    expect(result.current.activeFrom).toBe('')
    expect(result.current.activeTo).toBe('')
    expect(window.location.search).toBe('?q=Bicep')
  })

  it('Should_sync_dates_on_browser_popstate', () => {
    window.history.replaceState(null, '', '/?from=2026-10-10&to=2026-10-12')
    const { result } = renderHook(() => useEventSearchState())

    expect(result.current.activeFrom).toBe('2026-10-10')
    expect(result.current.activeTo).toBe('2026-10-12')

    act(() => {
      window.history.replaceState(null, '', '/?from=2026-11-01&to=2026-11-03')
      window.dispatchEvent(new PopStateEvent('popstate'))
    })

    expect(result.current.activeFrom).toBe('2026-11-01')
    expect(result.current.activeTo).toBe('2026-11-03')
  })
})
