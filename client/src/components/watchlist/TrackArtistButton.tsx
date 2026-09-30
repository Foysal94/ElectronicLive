interface TrackArtistButtonProps {
  artistName: string
  onClick: () => void
  className?: string
}

export function TrackArtistButton({
  artistName,
  onClick,
  className = '',
}: TrackArtistButtonProps) {
  const displayLabel = `Track ${artistName}`

  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={`Track ${artistName}`}
      className={`inline-flex items-center justify-center gap-2 min-h-[44px] px-4 py-2 rounded-xl text-sm font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500/20 hover:border-emerald-500/50 active:scale-[0.98] transition-all cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-400 ${className}`}
    >
      <svg
        className="w-4 h-4 text-emerald-400 flex-shrink-0"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
        />
      </svg>
      <span>{displayLabel}</span>
    </button>
  )
}
