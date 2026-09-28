import { Header } from '../components/layout/Header'
import { SearchBar } from '../components/search/SearchBar'
import { QuickPills } from '../components/search/QuickPills'
import { EventList } from '../components/events/EventList'
import { useEventsSearch } from '../hooks/useEventsSearch'
import { useEventSearchState } from '../hooks/useEventSearchState'

export default function App() {
  const {
    searchTerm,
    activeQuery,
    activeGenre,
    setSearchTerm,
    handleSearch,
    handleSelectGenre,
    handleClear,
  } = useEventSearchState()

  const { events, isFetching, isError, error, refetch, isIdle } = useEventsSearch({
    query: activeQuery,
    genre: activeGenre || undefined,
  })

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
            onSelectQuery={handleSearch}
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
