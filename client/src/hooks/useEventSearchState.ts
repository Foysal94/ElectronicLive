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
 * Custom hook managing search input state, address bar synchronization, and filter exclusivity.
 */
export function useEventSearchState(): EventSearchState {
  const [urlState, setUrlState] = useState<ParsedUrlState>(parseSearchFromUrl)
  const [searchTerm, setSearchTerm] = useState<string>(() => parseSearchFromUrl().query)

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

    if (typeof window === 'undefined') return
    // Use replaceState to keep browser history tidy rather than pushing entries on every search
    const newUrl = trimmed
      ? `${window.location.pathname}?q=${encodeURIComponent(trimmed)}`
      : window.location.pathname
    window.history.replaceState(null, '', newUrl)
  }

  const handleSelectGenre = (genre: EventGenre) => {
    setUrlState({ query: '', genre })
    setSearchTerm('')

    if (typeof window === 'undefined') return
    const newUrl = `${window.location.pathname}?genre=${encodeURIComponent(genre)}`
    window.history.replaceState(null, '', newUrl)
  }

  const handleClear = () => {
    setUrlState({ query: '', genre: '' })
    setSearchTerm('')

    if (typeof window === 'undefined') return
    window.history.replaceState(null, '', window.location.pathname)
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
