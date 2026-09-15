interface HeaderProps {
  className?: string
}

export function Header({ className = '' }: HeaderProps) {
  return (
    <header
      className={`w-full border-b border-white/10 bg-[#181b1f]/95 backdrop-blur-sm py-4 px-4 sm:px-6 ${className}`}
    >
      <div className="max-w-5xl mx-auto flex flex-col items-center text-center gap-2">
        <div className="flex items-center justify-center gap-3">
          <div className="flex items-center gap-2">
            <svg
              className="w-7 h-7 text-emerald-400"
              viewBox="0 0 24 24"
              fill="currentColor"
              aria-hidden="true"
            >
              <rect x="2" y="9" width="2" height="6" rx="1" />
              <rect x="6" y="5" width="2" height="14" rx="1" />
              <rect x="10" y="2" width="2" height="20" rx="1" />
              <rect x="14" y="6" width="2" height="12" rx="1" />
              <rect x="18" y="4" width="2" height="16" rx="1" />
              <rect x="22" y="9" width="2" height="6" rx="1" />
            </svg>
            <span className="text-xl sm:text-2xl font-bold tracking-tight text-[#f3f4f6]">
              ElectronicLive
            </span>
          </div>

          <span
            className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-[#22262d] text-gray-300 border border-white/10 shadow-sm"
            aria-label="City scope: London, UK"
          >
            📍 London, UK
          </span>
        </div>

        <p className="text-xs sm:text-sm text-[#9ca3af] max-w-xl">
          Aggregating live events from{' '}
          <span className="text-gray-300 font-medium">Resident Advisor</span> ·{' '}
          <span className="text-gray-300 font-medium">Ticketmaster</span> ·{' '}
          <span className="text-gray-300 font-medium">Skiddle</span>
        </p>
      </div>
    </header>
  )
}
