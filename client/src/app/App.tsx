import { useState } from 'react'
import { Header } from '../components/layout/Header'
import { SearchBar } from '../components/search/SearchBar'
import { QuickPills } from '../components/search/QuickPills'
import { EventList } from '../components/events/EventList'
import { useEventsSearch } from '../hooks/useEventsSearch'
import { useSearchParam } from '../hooks/useSearchParam'

export default function App() {
  const [activeQuery, setActiveQuery] = useSearchParam('q')
  const [searchTerm, setSearchTerm] = useState<string>(activeQuery)
  const [prevActiveQuery, setPrevActiveQuery] = useState<string>(activeQuery)

  if (prevActiveQuery !== activeQuery) {
    setPrevActiveQuery(activeQuery)
    setSearchTerm(activeQuery)
  }

  const { events, isFetching, isError, error, refetch, isIdle } = useEventsSearch(activeQuery)

  const handleSearch = (query: string) => {
    setActiveQuery(query)
  }

  const handleSelectPill = (pill: string) => {
    setActiveQuery(pill)
  }

  const handleClear = () => {
    setActiveQuery('')
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
