import type { FormEvent } from 'react'

interface SearchBarProps {
  value: string
  onChange: (value: string) => void
  onSearch: (query: string) => void
  onClear?: () => void
  className?: string
}

export function SearchBar({
  value,
  onChange,
  onSearch,
  onClear,
  className = '',
}: SearchBarProps) {
  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    onSearch(value.trim())
  }

  const handleClear = () => {
    onChange('')
    onClear?.()
  }

  return (
    <form
      role="search"
      onSubmit={handleSubmit}
      className={`w-full max-w-2xl mx-auto flex items-center gap-2.5 ${className}`}
    >
      <div className="relative flex-1">
        <input
          type="search"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="Search artist, event, or venue in London..."
          aria-label="Search artist, event, or venue in London"
          className="w-full min-h-[44px] px-4 py-2.5 pr-10 rounded-xl bg-[#22262d] text-[#f3f4f6] placeholder-gray-400 border border-white/10 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 text-sm sm:text-base transition-colors shadow-inner"
        />
        {value.length > 0 && (
          <button
            type="button"
            onClick={handleClear}
            aria-label="Clear search"
            className="absolute right-1.5 top-1/2 -translate-y-1/2 min-h-[44px] min-w-[44px] flex items-center justify-center text-gray-400 hover:text-white transition-colors"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        )}
      </div>

      <button
        type="submit"
        className="min-h-[44px] px-5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white font-medium text-sm sm:text-base transition-colors focus:outline-none focus:ring-2 focus:ring-emerald-400 shadow-sm flex items-center justify-center flex-shrink-0"
      >
        Search
      </button>
    </form>
  )
}
