import type { EventGenre } from '../../api/types'
import { QUICK_ARTISTS, QUICK_GENRES, QUICK_VENUES } from './constants'

export interface QuickPillsProps {
  activeQuery?: string
  activeGenre?: EventGenre | ''
  onSelect?: (query: string) => void
  onSelectQuery?: (query: string) => void
  onSelectGenre?: (genre: EventGenre) => void
  className?: string
}

export function QuickPills({
  activeQuery = '',
  activeGenre = '',
  onSelect,
  onSelectQuery,
  onSelectGenre,
  className = '',
}: QuickPillsProps) {
  const normalizedActive = activeQuery.trim().toLowerCase()

  const handleQuerySelect = (label: string) => {
    if (onSelectQuery) {
      onSelectQuery(label)
    } else if (onSelect) {
      onSelect(label)
    }
  }

  const renderTextPill = (label: string) => {
    const isActive = !activeGenre && normalizedActive === label.toLowerCase()

    return (
      <button
        key={label}
        type="button"
        onClick={() => handleQuerySelect(label)}
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
          {QUICK_ARTISTS.map(renderTextPill)}
        </div>
      </div>

      <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3">
        <span className="text-xs font-semibold uppercase tracking-wider text-gray-400 flex-shrink-0">
          Quick Search Venues:
        </span>
        <div className="flex flex-wrap gap-2">
          {QUICK_VENUES.map(renderTextPill)}
        </div>
      </div>

      <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3">
        <span className="text-xs font-semibold uppercase tracking-wider text-gray-400 flex-shrink-0">
          Quick Search Genres:
        </span>
        <div className="flex flex-wrap gap-2">
          {QUICK_GENRES.map((item) => {
            const isActive = activeGenre === item.value

            return (
              <button
                key={item.value}
                type="button"
                onClick={() => onSelectGenre?.(item.value)}
                className={`min-h-[44px] px-4 py-2 rounded-full text-xs sm:text-sm font-medium border transition-colors flex items-center justify-center ${
                  isActive
                    ? 'bg-emerald-950 text-emerald-400 border-emerald-800 shadow-sm'
                    : 'bg-[#22262d] text-gray-300 border-white/10 hover:bg-[#2b3039] hover:text-white hover:border-white/20'
                }`}
              >
                {item.label}
              </button>
            )
          })}
        </div>
      </div>
    </section>
  )
}

