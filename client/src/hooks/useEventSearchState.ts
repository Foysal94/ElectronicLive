import { useEffect, useState } from 'react'
import { type EventGenre, isEventGenre } from '../api/types'

export interface EventSearchState {
  searchTerm: string
  activeQuery: string
  activeGenre: EventGenre | ''
  setSearchTerm: (term: string) => void
  handleSearch: (query: string) => void
  handleSelectGenre: (genre: EventGenre) => void
  handleClear: () => void
}

interface ParsedUrlState {
  query: string
  genre: EventGenre | ''
}

/**
 * Extracts initial search state from the browser URL.
 * Enforces mutual exclusivity: if a valid ?genre= is present, it takes precedence
 * over free-text ?q= to prevent mixed or conflicting search states.
 */
function parseSearchFromUrl(): ParsedUrlState {
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
 * Synchronizes search state with the browser address bar using replaceState
 * to keep navigation history tidy without pushing new entries on filter tweaks.
 */
function syncUrl(query: string, genre: EventGenre | '') {
  if (typeof window === 'undefined') return

  let newUrl = window.location.pathname
  if (genre) {
    newUrl = `${window.location.pathname}?genre=${encodeURIComponent(genre)}`
  } else if (query) {
    newUrl = `${window.location.pathname}?q=${encodeURIComponent(query)}`
  }

  window.history.replaceState(null, '', newUrl)
}

/**
 * Custom hook managing search input state, address bar synchronization, and filter exclusivity.
 */
export function useEventSearchState(): EventSearchState {
  const [urlState, setUrlState] = useState<ParsedUrlState>(parseSearchFromUrl)
  const [searchTerm, setSearchTerm] = useState<string>(() => urlState.query)

  // Listen to browser Back/Forward navigation ('popstate') so input and active filters match history
  useEffect(() => {
    const handlePopState = () => {
      const parsed = parseSearchFromUrl()
      setUrlState(parsed)
      setSearchTerm(parsed.query)
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const handleSearch = (rawQuery: string) => {
    const trimmed = rawQuery.trim()
    setUrlState({ query: trimmed, genre: '' })
    setSearchTerm(trimmed)
    syncUrl(trimmed, '')
  }

  const handleSelectGenre = (genre: EventGenre) => {
    setUrlState({ query: '', genre })
    setSearchTerm('')
    syncUrl('', genre)
  }

  const handleClear = () => {
    setUrlState({ query: '', genre: '' })
    setSearchTerm('')
    syncUrl('', '')
  }

  return {
    searchTerm,
    activeQuery: urlState.query,
    activeGenre: urlState.genre,
    setSearchTerm,
    handleSearch,
    handleSelectGenre,
    handleClear,
  }
}
