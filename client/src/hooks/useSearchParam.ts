import { useEffect, useState } from 'react'
import { type EventGenre, isEventGenre } from '../api/types'

export interface AppSearchState {
  query: string
  genre: EventGenre | ''
}

/**
 * Extracts initial search state from the browser URL on page load.
 * Enforces mutual exclusivity: if a valid ?genre= is present, it takes precedence
 * over free-text ?q= to prevent mixed or conflicting search states.
 */
export function parseSearchFromUrl(): AppSearchState {
  if (typeof window === 'undefined') {
    return { query: '', genre: '' }
  }

  const searchParams = new URLSearchParams(window.location.search)
  const genreParam = searchParams.get('genre')?.trim().toLowerCase()
  if (genreParam && isEventGenre(genreParam)) {
    return { query: '', genre: genreParam }
  }

  const queryParam = searchParams.get('q')?.trim() || ''
  return { query: queryParam, genre: '' }
}

/**
 * Custom hook synchronizing search state (artist/venue text vs. genre pill)
 * with the browser's address bar without requiring an external routing library.
 */
export function useAppSearchParams() {
  const [searchState, setSearchState] = useState<AppSearchState>(parseSearchFromUrl)

  // Listen to browser Back/Forward navigation ('popstate') so the UI stays in sync with URL history
  useEffect(() => {
    const handlePopState = () => {
      setSearchState(parseSearchFromUrl())
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  // Switches to text query mode; clears any active genre filter
  const setQuery = (query: string) => {
    const trimmed = query.trim()
    setSearchState({ query: trimmed, genre: '' })

    if (typeof window === 'undefined') return
    // Use replaceState to keep browser history tidy rather than pushing a new entry on every keystroke/preset
    const newUrl = trimmed
      ? `${window.location.pathname}?q=${encodeURIComponent(trimmed)}`
      : window.location.pathname
    window.history.replaceState(null, '', newUrl)
  }

  // Switches to genre filter mode; clears any active text search
  const setGenre = (genre: EventGenre) => {
    setSearchState({ query: '', genre })

    if (typeof window === 'undefined') return
    const newUrl = `${window.location.pathname}?genre=${encodeURIComponent(genre)}`
    window.history.replaceState(null, '', newUrl)
  }

  // Resets all filters back to the idle / empty state
  const clearAll = () => {
    setSearchState({ query: '', genre: '' })

    if (typeof window === 'undefined') return
    window.history.replaceState(null, '', window.location.pathname)
  }

  return {
    query: searchState.query,
    genre: searchState.genre,
    setQuery,
    setGenre,
    clearAll,
  }
}

/**
 * Generic single-parameter URL search param hook for simple key-value synchronization.
 */
export function useSearchParam(param = 'q') {
  const [value, setValue] = useState<string>(() => {
    if (typeof window === 'undefined') return ''
    return new URLSearchParams(window.location.search).get(param) || ''
  })

  useEffect(() => {
    const handlePopState = () => {
      const current = new URLSearchParams(window.location.search).get(param) || ''
      setValue(current)
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [param])

  const setParam = (nextValue: string) => {
    const trimmed = nextValue.trim()
    setValue(trimmed)
    if (typeof window === 'undefined') return

    const newUrl = trimmed
      ? `${window.location.pathname}?${param}=${encodeURIComponent(trimmed)}`
      : window.location.pathname

    window.history.replaceState(null, '', newUrl)
  }

  return [value, setParam] as const
}

