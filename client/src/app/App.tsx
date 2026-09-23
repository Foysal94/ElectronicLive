import { useState } from 'react'
import type { EventGenre } from '../api/types'
import { Header } from '../components/layout/Header'
import { SearchBar } from '../components/search/SearchBar'
import { QuickPills } from '../components/search/QuickPills'
import { EventList } from '../components/events/EventList'
import { useEventsSearch } from '../hooks/useEventsSearch'
import { useAppSearchParams } from '../hooks/useSearchParam'

export default function App() {
  const { query: activeQuery, genre: activeGenre, setQuery, setGenre, clearAll } = useAppSearchParams()
  const [searchTerm, setSearchTerm] = useState<string>(activeQuery)
  const [prevActiveQuery, setPrevActiveQuery] = useState<string>(activeQuery)
  const [prevActiveGenre, setPrevActiveGenre] = useState<string>(activeGenre)

  if (prevActiveQuery !== activeQuery || prevActiveGenre !== activeGenre) {
    setPrevActiveQuery(activeQuery)
    setPrevActiveGenre(activeGenre)
    setSearchTerm(activeQuery)
  }

  const { events, isFetching, isError, error, refetch, isIdle } = useEventsSearch({
    query: activeQuery,
    genre: activeGenre || undefined,
  })

  const handleSearch = (query: string) => {
    setQuery(query)
  }

  const handleSelectArtistVenue = (name: string) => {
    setQuery(name)
  }

  const handleSelectGenre = (genre: EventGenre) => {
    setGenre(genre)
  }

  const handleClear = () => {
    clearAll()
  }

  const displayQuery = activeQuery || (activeGenre ? activeGenre.toUpperCase() : '')

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
          <QuickPills
            activeQuery={activeQuery}
            activeGenre={activeGenre}
            onSelectQuery={handleSelectArtistVenue}
            onSelectGenre={handleSelectGenre}
          />
        </section>

        <section aria-label="Upcoming events timetable">
          <EventList
            events={events}
            isIdle={isIdle}
            isFetching={isFetching}
            isError={isError}
            error={error}
            onRetry={refetch}
            query={displayQuery}
          />
        </section>
      </main>
    </div>
  )
}

