import type { EventResponse } from '../../api/types'
import { ErrorBanner } from '../common/ErrorBanner'
import { EmptyState } from './EmptyState'
import { EventRow } from './EventRow'
import { LoadingState } from './LoadingState'
import { sortEventsChronologically } from './utils'

interface EventListProps {
  events: EventResponse[]
  isIdle: boolean
  isFetching: boolean
  isError: boolean
  error?: Error | null
  query?: string
  onRetry?: () => void
  className?: string
}

export function EventList({
  events,
  isIdle,
  isFetching,
  isError,
  error = null,
  query = '',
  onRetry,
  className = '',
}: EventListProps) {
  const renderContent = () => {
    if (isError) {
      return (
        <ErrorBanner
          message={error?.message || undefined}
          onRetry={onRetry}
        />
      )
    }

    if (isFetching) {
      return <LoadingState />
    }

    if (isIdle) {
      return <EmptyState isIdle />
    }

    if (events.length === 0) {
      return <EmptyState query={query} />
    }

    const sortedEvents = sortEventsChronologically(events)

    return (
      <div className="flex flex-col gap-3">
        <div className="flex items-center justify-between px-1 pb-1">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-gray-400">
            Upcoming Shows ({sortedEvents.length})
          </h2>
          <span className="text-xs text-gray-500 font-mono">Chronological</span>
        </div>

        <div className="flex flex-col gap-3">
          {sortedEvents.map((event) => (
            <EventRow key={event.id} event={event} />
          ))}
        </div>
      </div>
    )
  }

  return (
    <section
      aria-label="Upcoming events"
      className={`w-full max-w-4xl mx-auto ${className}`}
    >
      {renderContent()}
    </section>
  )
}
