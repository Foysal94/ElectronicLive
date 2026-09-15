import type { EventResponse } from '../../api/types'
import { ErrorBanner } from '../common/ErrorBoundary'
import { EmptyState } from './EmptyState'
import { EventRow } from './EventRow'
import { EventSkeleton } from './EventSkeleton'
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
  if (isError) {
    return (
      <div className={`w-full max-w-4xl mx-auto ${className}`}>
        <ErrorBanner
          message={error?.message || undefined}
          onRetry={onRetry}
        />
      </div>
    )
  }

  if (isFetching) {
    return (
      <section
        aria-label="Upcoming events"
        className={`w-full max-w-4xl mx-auto ${className}`}
      >
        <EventSkeleton count={4} />
      </section>
    )
  }

  if (isIdle) {
    return (
      <section
        aria-label="Upcoming events"
        className={`w-full max-w-4xl mx-auto ${className}`}
      >
        <EmptyState isIdle />
      </section>
    )
  }

  if (events.length === 0) {
    return (
      <section
        aria-label="Upcoming events"
        className={`w-full max-w-4xl mx-auto ${className}`}
      >
        <EmptyState query={query} />
      </section>
    )
  }

  const sortedEvents = sortEventsChronologically(events)

  return (
    <section
      aria-label="Upcoming events"
      className={`w-full max-w-4xl mx-auto flex flex-col gap-3 ${className}`}
    >
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
    </section>
  )
}
