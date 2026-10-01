import { useEffect, useRef, useState } from 'react'
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

function sanitizeDateParam(param: string | null | undefined): string {
  const trimmed = param?.trim() || ''
  if (!ISO_DATE_REGEX.test(trimmed)) {
    return ''
  }

  const [y, m, d] = trimmed.split('-').map(Number)
  const date = new Date(Date.UTC(y, m - 1, d))
  return date.getUTCFullYear() === y &&
    date.getUTCMonth() === m - 1 &&
    date.getUTCDate() === d
    ? trimmed
    : ''
}

function sanitizeDateRange(
  fromParam: string | null | undefined,
  toParam: string | null | undefined
): { from: string; to: string } {
  const from = sanitizeDateParam(fromParam)
  const to = sanitizeDateParam(toParam)

  // Invariant: 'to' must be greater than or equal to 'from' when both bounds are provided
  if (from && to && from > to) {
    return { from: '', to: '' }
  }

  return { from, to }
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
  const { from, to } = sanitizeDateRange(searchParams.get('from'), searchParams.get('to'))

  const genreParam = searchParams.get('genre')?.trim().toLowerCase()
  if (genreParam && isEventGenre(genreParam)) {
    return { query: '', genre: genreParam, from, to }
  }

  const queryParam = searchParams.get('q')?.trim() || ''
  return { query: queryParam, genre: '', from, to }
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
  const basePath = qs ? `${window.location.pathname}?${qs}` : window.location.pathname
  const newUrl = `${basePath}${window.location.hash || ''}`

  window.history.replaceState(null, '', newUrl)
}

export function useEventSearchState(): EventSearchState {
  const [urlState, setUrlState] = useState<ParsedUrlState>(parseSearchFromUrl)
  const [searchTerm, setSearchTerm] = useState<string>(() => urlState.query)
  const stateRef = useRef(urlState)

  useEffect(() => {
    stateRef.current = urlState
  }, [urlState])

  // Listen to browser Back/Forward navigation ('popstate') so input and active filters match history
  useEffect(() => {
    const handlePopState = () => {
      const parsed = parseSearchFromUrl()
      stateRef.current = parsed
      setUrlState(parsed)
      setSearchTerm(parsed.query)
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const handleSearch = (rawQuery: string) => {
    const trimmed = rawQuery.trim()
    const nextState = { ...stateRef.current, query: trimmed, genre: '' as const }
    stateRef.current = nextState
    syncUrl(trimmed, '', nextState.from, nextState.to)
    setUrlState(nextState)
    setSearchTerm(trimmed)
  }

  const handleSelectGenre = (genre: EventGenre) => {
    const nextState = { ...stateRef.current, query: '', genre }
    stateRef.current = nextState
    syncUrl('', genre, nextState.from, nextState.to)
    setUrlState(nextState)
    setSearchTerm('')
  }

  const setDateRange = (from?: string, to?: string) => {
    const { from: cleanFrom, to: cleanTo } = sanitizeDateRange(from, to)
    const nextState = { ...stateRef.current, from: cleanFrom, to: cleanTo }
    stateRef.current = nextState
    syncUrl(nextState.query, nextState.genre, cleanFrom, cleanTo)
    setUrlState(nextState)
  }

  const handleClear = () => {
    const nextState = { ...stateRef.current, query: '', genre: '' as const }
    stateRef.current = nextState
    syncUrl('', '', nextState.from, nextState.to)
    setUrlState(nextState)
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
