import type { EventResponse, EventTicketOffer } from '../../api/types'
import { DateBlock } from './DateBlock'
import { ProviderButton } from './ProviderButton'

interface EventRowProps {
  event: EventResponse
  className?: string
}

export function EventRow({ event, className = '' }: EventRowProps) {
  const resolvedOffers: readonly EventTicketOffer[] =
    event.offers && event.offers.length > 0
      ? event.offers
      : [{ provider: event.provider, ticketUrl: event.ticketUrl, status: event.status }]

  const providerCount = resolvedOffers.length

  // Responsive scaling: on mobile, 1 provider is full width, 2 are 50/50 grid,
  // 3 use 2+1 grid; on desktop (sm:), all collapse into flex side-by-side buttons.
  const renderOffers = () => {
    if (providerCount === 1) {
      const offer = resolvedOffers[0]
      if (!offer) return null
      return (
        <div className="w-full sm:w-auto">
          <ProviderButton
            provider={offer.provider}
            ticketUrl={offer.ticketUrl}
            status={offer.status}
            fullWidth
            className="sm:w-auto"
          />
        </div>
      )
    }

    if (providerCount === 2) {
      return (
        <div className="grid grid-cols-2 gap-2 w-full sm:flex sm:flex-wrap sm:items-center sm:justify-end sm:w-auto">
          {resolvedOffers.map((offer) => (
            <ProviderButton
              key={offer.provider}
              provider={offer.provider}
              ticketUrl={offer.ticketUrl}
              status={offer.status}
            />
          ))}
        </div>
      )
    }

    // 3 providers: mobile 2-column grid with 3rd button spanning 2 cols; desktop inline flex
    return (
      <div className="grid grid-cols-2 gap-2 w-full sm:flex sm:flex-wrap sm:items-center sm:justify-end sm:w-auto">
        {resolvedOffers.map((offer, index) => (
          <div
            key={offer.provider}
            className={index === 2 ? 'col-span-2 sm:col-span-1 sm:w-auto' : 'w-full sm:w-auto'}
          >
            <ProviderButton
              provider={offer.provider}
              ticketUrl={offer.ticketUrl}
              status={offer.status}
              fullWidth={index === 2}
              className="sm:w-auto"
            />
          </div>
        ))}
      </div>
    )
  }

  return (
    <article
      className={`rounded-2xl border border-white/10 bg-[#22262d] p-3.5 sm:p-4 hover:border-white/20 transition-colors shadow-md flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 sm:gap-4 ${className}`}
    >
      <div className="flex items-center gap-3 sm:gap-4 min-w-0 flex-1">
        <DateBlock date={event.date} time={event.time} />
        <div className="min-w-0 flex-1">
          <h3 className="text-sm sm:text-lg font-bold text-[#f3f4f6] line-clamp-2 sm:line-clamp-none sm:truncate">
            {event.name}
          </h3>
          <p className="text-xs sm:text-sm text-[#9ca3af] truncate flex items-center gap-1 sm:gap-1.5 mt-0.5 sm:mt-1">
            <span>📍</span>
            <span className="truncate">{event.venueName}</span>
          </p>
        </div>
      </div>

      <div className="pt-2.5 border-t border-white/5 sm:pt-0 sm:border-0 sm:max-w-sm flex-shrink-0">
        {renderOffers()}
      </div>
    </article>
  )
}
