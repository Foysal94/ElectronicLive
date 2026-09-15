import type { EventProvider, EventStatus } from '../../api/types'
import { Badge } from '../common/Badge'

interface ProviderButtonProps {
  provider: EventProvider
  ticketUrl: string | null
  status: EventStatus
  fullWidth?: boolean
  className?: string
}

const PROVIDER_NAMES: Record<EventProvider, string> = {
  ResidentAdvisor: 'Resident Advisor',
  Ticketmaster: 'Ticketmaster',
  Skiddle: 'Skiddle',
}

export function ProviderButton({
  provider,
  ticketUrl,
  status,
  fullWidth = false,
  className = '',
}: ProviderButtonProps) {
  const providerName = PROVIDER_NAMES[provider] ?? provider
  const hasValidUrl = typeof ticketUrl === 'string' && ticketUrl.trim().length > 0

  if (!hasValidUrl) {
    return (
      <button
        type="button"
        disabled
        aria-label={`${providerName}: Tickets TBA`}
        className={`min-h-[44px] px-3.5 py-2 rounded-xl bg-[#22262d] text-gray-400 border border-white/5 opacity-60 cursor-not-allowed flex items-center justify-between gap-2 text-xs sm:text-sm font-medium ${
          fullWidth ? 'w-full' : ''
        } ${className}`}
      >
        <span className="truncate">{providerName}</span>
        <span className="text-[11px] text-gray-400 font-normal">TBA</span>
      </button>
    )
  }

  return (
    <a
      href={ticketUrl}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={`Get tickets on ${providerName} (${status}) - opens in new tab`}
      className={`min-h-[44px] px-3.5 py-2 rounded-xl bg-[#2b3039] hover:bg-[#343a46] active:bg-[#22262d] text-[#f3f4f6] border border-white/10 hover:border-white/25 transition-all flex items-center justify-between gap-2 text-xs sm:text-sm font-medium shadow-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 ${
        fullWidth ? 'w-full' : ''
      } ${className}`}
    >
      <span className="flex items-center gap-1.5 truncate">
        <span className="truncate">{providerName}</span>
        <span className="text-gray-400 text-xs" aria-hidden="true">
          ↗
        </span>
      </span>
      <Badge status={status} />
    </a>
  )
}
