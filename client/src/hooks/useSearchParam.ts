import { useEffect, useState } from 'react'

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
