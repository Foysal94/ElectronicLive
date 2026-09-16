import { useEffect, useState } from 'react'
import { Header } from '../components/layout/Header'
import { SearchBar } from '../components/search/SearchBar'
import { QuickPills } from '../components/search/QuickPills'
import { EventList } from '../components/events/EventList'
import { useEventsSearch } from '../hooks/useEventsSearch'

function getInitialQuery(): string {
  if (typeof window === 'undefined') return ''
  return new URLSearchParams(window.location.search).get('q') || ''
}

function updateUrlQuery(query: string): void {
  if (typeof window === 'undefined') return
  const trimmed = query.trim()
  const newUrl = trimmed
    ? `${window.location.pathname}?q=${encodeURIComponent(trimmed)}`
    : window.location.pathname
  window.history.replaceState(null, '', newUrl)
}

export default function App() {
  const [searchTerm, setSearchTerm] = useState<string>(getInitialQuery)
  const [activeQuery, setActiveQuery] = useState<string>(getInitialQuery)

  const { events, isFetching, isError, error, refetch, isIdle } = useEventsSearch(activeQuery)

  useEffect(() => {
    const handlePopState = () => {
      const q = getInitialQuery()
      setSearchTerm(q)
      setActiveQuery(q)
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const handleSearch = (query: string) => {
    const trimmed = query.trim()
    setActiveQuery(trimmed)
    updateUrlQuery(trimmed)
  }

  const handleSelectPill = (pill: string) => {
    const trimmed = pill.trim()
    setSearchTerm(trimmed)
    setActiveQuery(trimmed)
    updateUrlQuery(trimmed)
  }

  const handleClear = () => {
    setSearchTerm('')
    setActiveQuery('')
    updateUrlQuery('')
  }

  return (
    <div className="min-h-screen bg-[#181b1f] text-[#f3f4f6] flex flex-col selection:bg-emerald-500/20 selection:text-emerald-300">
      <Header />
      <main className="flex-1 max-w-5xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
        <section aria-label="Event search" className="space-y-4">
          <SearchBar
            value={searchTerm}
            onChange={setSearchTerm}
            onSearch={handleSearch}
            onClear={handleClear}
          />
          <QuickPills activeQuery={activeQuery} onSelect={handleSelectPill} />
        </section>

        <section aria-label="Upcoming events timetable">
          <EventList
            events={events}
            isIdle={isIdle}
            isFetching={isFetching}
            isError={isError}
            error={error}
            onRetry={refetch}
            query={activeQuery}
          />
        </section>
      </main>
    </div>
  )
}
