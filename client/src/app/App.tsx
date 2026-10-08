import { useEffect, useState } from 'react'
import { pingHealth } from '../api/client'
import { Header } from '../components/layout/Header'
import { SearchBar } from '../components/search/SearchBar'
import { DateFilterBar } from '../components/search/DateFilterBar'
import { QuickPills } from '../components/search/QuickPills'
import { EventList } from '../components/events/EventList'
import { TrackArtistModal } from '../components/watchlist/TrackArtistModal'
import { useEventsSearch } from '../hooks/useEventsSearch'
import { useEventSearchState } from '../hooks/useEventSearchState'

export default function App() {
  const [trackingArtist, setTrackingArtist] = useState<string | null>(null)

  // Pre-warms idle backend containers on page load to hide cold-start latency.
  // Cancels cleanly on unmount to prevent duplicate in-flight requests.
  useEffect(() => {
    const controller = new AbortController()
    void pingHealth(controller.signal)
    return () => {
      controller.abort()
    }
  }, [])

  const {
    searchTerm,
    activeQuery,
    activeGenre,
    activeFrom,
    activeTo,
    setSearchTerm,
    handleSearch,
    handleSelectGenre,
    setDateRange,
    handleClear,
  } = useEventSearchState()

  const { events, isFetching, isError, error, refetch, isIdle } = useEventsSearch({
    query: activeQuery,
    genre: activeGenre || undefined,
    from: activeFrom || undefined,
    to: activeTo || undefined,
  })

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
          <DateFilterBar
            activeFrom={activeFrom}
            activeTo={activeTo}
            onSelectDateRange={setDateRange}
            hasSearchContext={Boolean(activeQuery || activeGenre)}
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
            query={activeQuery}
            onTrackArtist={(artist) => setTrackingArtist(artist)}
          />
        </section>
      </main>

      <TrackArtistModal
        isOpen={Boolean(trackingArtist)}
        artistName={trackingArtist || ''}
        onClose={() => setTrackingArtist(null)}
      />
    </div>
  )
}
