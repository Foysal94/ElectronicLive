import { QUICK_ARTISTS, QUICK_VENUES } from './constants'

interface QuickPillsProps {
  activeQuery?: string
  onSelect: (query: string) => void
  className?: string
}

export function QuickPills({
  activeQuery = '',
  onSelect,
  className = '',
}: QuickPillsProps) {
  const normalizedActive = activeQuery.trim().toLowerCase()

  const renderPill = (label: string) => {
    const isActive = normalizedActive === label.toLowerCase()

    return (
      <button
        key={label}
        type="button"
        onClick={() => onSelect(label)}
        className={`min-h-[44px] px-4 py-2 rounded-full text-xs sm:text-sm font-medium border transition-colors flex items-center justify-center ${
          isActive
            ? 'bg-emerald-950 text-emerald-400 border-emerald-800 shadow-sm'
            : 'bg-[#22262d] text-gray-300 border-white/10 hover:bg-[#2b3039] hover:text-white hover:border-white/20'
        }`}
      >
        {label}
      </button>
    )
  }

  return (
    <section
      aria-label="Quick search filters"
      className={`w-full max-w-4xl mx-auto flex flex-col gap-3 text-left ${className}`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3">
        <span className="text-xs font-semibold uppercase tracking-wider text-gray-400 flex-shrink-0">
          Quick Search Artists:
        </span>
        <div className="flex flex-wrap gap-2">
          {QUICK_ARTISTS.map(renderPill)}
        </div>
      </div>

      <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3">
        <span className="text-xs font-semibold uppercase tracking-wider text-gray-400 flex-shrink-0">
          Quick Search Venues:
        </span>
        <div className="flex flex-wrap gap-2">
          {QUICK_VENUES.map(renderPill)}
        </div>
      </div>
    </section>
  )
}
