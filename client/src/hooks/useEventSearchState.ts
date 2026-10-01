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

const ISO_DATE_REGEX = /^\d{4}-\d{2}-\d{2}$/

function sanitizeDateParam(param: string | null): string {
  const trimmed = param?.trim() || ''
  return ISO_DATE_REGEX.test(trimmed) ? trimmed : ''
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
  const fromParam = sanitizeDateParam(searchParams.get('from'))
  const toParam = sanitizeDateParam(searchParams.get('to'))

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
    syncUrl(trimmed, '', urlState.from, urlState.to)
    setUrlState((prev) => ({ ...prev, query: trimmed, genre: '' }))
    setSearchTerm(trimmed)
  }

  const handleSelectGenre = (genre: EventGenre) => {
    syncUrl('', genre, urlState.from, urlState.to)
    setUrlState((prev) => ({ ...prev, query: '', genre }))
    setSearchTerm('')
  }

  const setDateRange = (from?: string, to?: string) => {
    const cleanFrom = sanitizeDateParam(from ?? null)
    const cleanTo = sanitizeDateParam(to ?? null)
    syncUrl(urlState.query, urlState.genre, cleanFrom, cleanTo)
    setUrlState((prev) => ({ ...prev, from: cleanFrom, to: cleanTo }))
  }

  const handleClear = () => {
    syncUrl('', '', urlState.from, urlState.to)
    setUrlState((prev) => ({ ...prev, query: '', genre: '' }))
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
