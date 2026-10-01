import { useEffect, useState } from 'react'
import { type EventGenre, isEventGenre } from '../api/types'

export interface EventSearchState {
  searchTerm: string
  activeQuery: string
  activeGenre: EventGenre | ''
  activeFrom: string
  activeTo: string
  setSearchTerm: (term: string) => void
  handleSearch: (query: string) => void
  handleSelectGenre: (genre: EventGenre) => void
  setDateRange: (from?: string, to?: string) => void
  handleClear: () => void
}

interface ParsedUrlState {
  query: string
  genre: EventGenre | ''
  from: string
  to: string
}

/**
 * Extracts initial search and date filter state from the browser URL.
 * Preserves mutual exclusivity between genre and query, while date bounds (from/to)
 * operate orthogonally alongside them.
 */
function parseSearchFromUrl(): ParsedUrlState {
  if (typeof window === 'undefined') {
    return { query: '', genre: '', from: '', to: '' }
  }

  const searchParams = new URLSearchParams(window.location.search)
  const fromParam = searchParams.get('from')?.trim() || ''
  const toParam = searchParams.get('to')?.trim() || ''

  const genreParam = searchParams.get('genre')?.trim().toLowerCase()
  if (genreParam && isEventGenre(genreParam)) {
    return { query: '', genre: genreParam, from: fromParam, to: toParam }
  }

  const queryParam = searchParams.get('q')?.trim() || ''
  return { query: queryParam, genre: '', from: fromParam, to: toParam }
}

/**
 * Synchronizes search and date range state with the browser address bar using replaceState
 * to keep navigation history tidy without pushing new entries on filter tweaks.
 */
function syncUrl(query: string, genre: EventGenre | '', from: string, to: string) {
  if (typeof window === 'undefined') return

  const queryParts: string[] = []
  if (genre) {
    queryParts.push(`genre=${encodeURIComponent(genre)}`)
  } else if (query) {
    queryParts.push(`q=${encodeURIComponent(query)}`)
  }

  if (from) {
    queryParts.push(`from=${encodeURIComponent(from)}`)
  }
  if (to) {
    queryParts.push(`to=${encodeURIComponent(to)}`)
  }

  const qs = queryParts.join('&')
  const newUrl = qs ? `${window.location.pathname}?${qs}` : window.location.pathname

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
    setUrlState((prev) => {
      syncUrl(trimmed, '', prev.from, prev.to)
      return { ...prev, query: trimmed, genre: '' }
    })
    setSearchTerm(trimmed)
  }

  const handleSelectGenre = (genre: EventGenre) => {
    setUrlState((prev) => {
      syncUrl('', genre, prev.from, prev.to)
      return { ...prev, query: '', genre }
    })
    setSearchTerm('')
  }

  const setDateRange = (from?: string, to?: string) => {
    const cleanFrom = from?.trim() || ''
    const cleanTo = to?.trim() || ''
    setUrlState((prev) => {
      syncUrl(prev.query, prev.genre, cleanFrom, cleanTo)
      return { ...prev, from: cleanFrom, to: cleanTo }
    })
  }

  const handleClear = () => {
    setUrlState((prev) => {
      syncUrl('', '', prev.from, prev.to)
      return { ...prev, query: '', genre: '' }
    })
    setSearchTerm('')
  }

  return {
    searchTerm,
    activeQuery: urlState.query,
    activeGenre: urlState.genre,
    activeFrom: urlState.from,
    activeTo: urlState.to,
    setSearchTerm,
    handleSearch,
    handleSelectGenre,
    setDateRange,
    handleClear,
  }
}
