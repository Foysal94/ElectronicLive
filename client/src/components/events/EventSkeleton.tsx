interface EventSkeletonProps {
  count?: number
  className?: string
  ariaHidden?: boolean
}

// Renders pulsing placeholder wireframes during active network fetches.
// Mirrors the exact structural dimensions and layout geometry of EventRow
// (date box, multi-line titles, button bounds) to eliminate Cumulative Layout Shift (CLS).
export function EventSkeleton({ count = 4, className = '', ariaHidden = false }: EventSkeletonProps) {
  const items = Array.from({ length: count }, (_, index) => index)

  return (
    <div
      role={ariaHidden ? undefined : 'status'}
      aria-label={ariaHidden ? undefined : 'Loading upcoming events'}
      aria-hidden={ariaHidden ? true : undefined}
      className={`w-full flex flex-col gap-3 ${className}`}
    >
      {items.map((key) => (
        <div
          key={key}
          className="rounded-2xl border border-white/5 bg-[#22262d]/60 p-3.5 sm:p-4 animate-pulse flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm"
        >
          <div className="flex items-center gap-3.5 flex-1">
            <div className="w-[72px] sm:w-[80px] h-[72px] sm:h-[80px] rounded-xl bg-white/5 flex-shrink-0" />
            <div className="flex-1 space-y-2.5">
              <div className="h-4 sm:h-5 bg-white/10 rounded-md w-3/4 max-w-sm" />
              <div className="h-3 sm:h-4 bg-white/5 rounded-md w-1/2 max-w-xs" />
            </div>
          </div>

          <div className="flex items-center gap-2 sm:self-center">
            <div className="h-10 w-full sm:w-36 bg-white/10 rounded-xl" />
          </div>
        </div>
      ))}
      {!ariaHidden && <span className="sr-only">Loading events...</span>}
    </div>
  )
}
