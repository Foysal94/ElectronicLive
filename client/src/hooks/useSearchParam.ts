import { useEffect, useState } from 'react'
import { type EventGenre, isEventGenre } from '../api/types'

export interface AppSearchState {
  query: string
  genre: EventGenre | ''
}

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

export function useAppSearchParams() {
  const [searchState, setSearchState] = useState<AppSearchState>(parseSearchFromUrl)

  useEffect(() => {
    const handlePopState = () => {
      setSearchState(parseSearchFromUrl())
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const setQuery = (query: string) => {
    const trimmed = query.trim()
    setSearchState({ query: trimmed, genre: '' })

    if (typeof window === 'undefined') return
    const newUrl = trimmed
      ? `${window.location.pathname}?q=${encodeURIComponent(trimmed)}`
      : window.location.pathname
    window.history.replaceState(null, '', newUrl)
  }

  const setGenre = (genre: EventGenre) => {
    setSearchState({ query: '', genre })

    if (typeof window === 'undefined') return
    const newUrl = `${window.location.pathname}?genre=${encodeURIComponent(genre)}`
    window.history.replaceState(null, '', newUrl)
  }

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

