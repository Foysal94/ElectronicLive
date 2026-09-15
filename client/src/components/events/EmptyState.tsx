interface EmptyStateProps {
  isIdle?: boolean
  query?: string
  className?: string
}

export function EmptyState({
  isIdle = false,
  query = '',
  className = '',
}: EmptyStateProps) {
  if (isIdle) {
    return (
      <div
        className={`rounded-2xl border border-white/5 bg-[#22262d]/50 p-8 sm:p-12 text-center max-w-xl mx-auto flex flex-col items-center justify-center gap-3 ${className}`}
      >
        <div className="w-12 h-12 rounded-full bg-emerald-950/60 border border-emerald-800/60 flex items-center justify-center text-emerald-400 text-xl">
          ⚡
        </div>
        <h3 className="text-base sm:text-lg font-bold text-white">
          Discover London Electronic Music
        </h3>
        <p className="text-sm text-[#9ca3af] max-w-md">
          Select a London artist or club above, or search to view upcoming shows.
        </p>
      </div>
    )
  }

  return (
    <div
      role="status"
      className={`rounded-2xl border border-white/5 bg-[#22262d]/50 p-8 sm:p-12 text-center max-w-xl mx-auto flex flex-col items-center justify-center gap-3 ${className}`}
    >
      <div className="w-12 h-12 rounded-full bg-zinc-800 border border-white/10 flex items-center justify-center text-gray-400 text-xl">
        🔍
      </div>
      <h3 className="text-base sm:text-lg font-bold text-white">
        No Gigs Found
      </h3>
      <p className="text-sm text-[#9ca3af] max-w-md">
        No upcoming London gigs found for{' '}
        <span className="text-white font-medium">"{query}"</span> across Resident
        Advisor, Ticketmaster, or Skiddle.
      </p>
      <p className="text-xs text-gray-500">
        Try searching for another artist or choose one of the curated venue pills above.
      </p>
    </div>
  )
}
