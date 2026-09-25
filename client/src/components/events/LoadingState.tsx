import { EventSkeleton } from './EventSkeleton'

interface LoadingStateProps {
  className?: string
}

export function LoadingState({ className = '' }: LoadingStateProps) {
  return (
    <div
      role="status"
      aria-live="polite"
      aria-label="Loading upcoming events"
      className={`w-full flex flex-col gap-4 ${className}`}
    >
      <div className="rounded-2xl border border-emerald-500/20 bg-[#22262d]/90 p-4 sm:p-5 flex items-center gap-3.5 shadow-md">
        <div className="w-10 h-10 rounded-xl bg-emerald-950/80 border border-emerald-500/40 flex items-center justify-center flex-shrink-0">
          <svg
            className="animate-spin w-5 h-5 text-emerald-400"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
        </div>

        <div className="flex-1 min-w-0">
          <h3 className="text-sm sm:text-base font-semibold text-white">
            Connecting to live feeds...
          </h3>
          <p className="text-xs sm:text-sm text-emerald-400 font-medium truncate sm:whitespace-normal">
            Aggregating shows from Resident Advisor, Ticketmaster & Skiddle
          </p>
        </div>
      </div>

      <EventSkeleton count={3} ariaHidden />
      <span className="sr-only">Loading events...</span>
    </div>
  )
}
