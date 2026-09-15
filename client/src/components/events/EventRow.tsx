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

  const renderMobileOffers = () => {
    if (providerCount === 1) {
      const offer = resolvedOffers[0]
      if (!offer) return null
      return (
        <div className="w-full">
          <ProviderButton
            provider={offer.provider}
            ticketUrl={offer.ticketUrl}
            status={offer.status}
            fullWidth
          />
        </div>
      )
    }

    if (providerCount === 2) {
      return (
        <div className="grid grid-cols-2 gap-2 w-full">
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

    // 3 providers: Row 1 has 2 buttons (50/50 split); Row 2 has 1 full-width button
    const firstTwo = resolvedOffers.slice(0, 2)
    const third = resolvedOffers[2]

    return (
      <div className="flex flex-col gap-2 w-full">
        <div className="grid grid-cols-2 gap-2 w-full">
          {firstTwo.map((offer) => (
            <ProviderButton
              key={offer.provider}
              provider={offer.provider}
              ticketUrl={offer.ticketUrl}
              status={offer.status}
            />
          ))}
        </div>
        {third && (
          <ProviderButton
            provider={third.provider}
            ticketUrl={third.ticketUrl}
            status={third.status}
            fullWidth
          />
        )}
      </div>
    )
  }

  return (
    <article
      className={`rounded-2xl border border-white/10 bg-[#22262d] p-3.5 sm:p-4 hover:border-white/20 transition-colors shadow-md ${className}`}
    >
      {/* Desktop Layout (>= 640px) */}
      <div className="hidden sm:flex sm:items-center sm:justify-between gap-4">
        <div className="flex items-center gap-4 min-w-0 flex-1">
          <DateBlock date={event.date} time={event.time} />
          <div className="min-w-0 flex-1">
            <h3 className="text-base sm:text-lg font-bold text-[#f3f4f6] truncate">
              {event.name}
            </h3>
            <p className="text-xs sm:text-sm text-[#9ca3af] truncate flex items-center gap-1.5 mt-0.5">
              <span>📍</span>
              <span className="truncate">{event.venueName}</span>
            </p>
          </div>
        </div>

        {/* Desktop Buttons Container (wraps cleanly on intermediate viewports) */}
        <div className="flex flex-wrap items-center justify-end gap-2 max-w-sm flex-shrink-0">
          {resolvedOffers.map((offer) => (
            <ProviderButton
              key={offer.provider}
              provider={offer.provider}
              ticketUrl={offer.ticketUrl}
              status={offer.status}
            />
          ))}
        </div>
      </div>

      {/* Mobile Layout (< 640px) */}
      <div className="flex flex-col gap-3 sm:hidden">
        <div className="flex items-center gap-3">
          <DateBlock date={event.date} time={event.time} />
          <div className="min-w-0 flex-1">
            <h3 className="text-sm font-bold text-[#f3f4f6] line-clamp-2">
              {event.name}
            </h3>
            <p className="text-xs text-[#9ca3af] truncate flex items-center gap-1 mt-1">
              <span>📍</span>
              <span className="truncate">{event.venueName}</span>
            </p>
          </div>
        </div>

        <div className="pt-1 border-t border-white/5">
          {renderMobileOffers()}
        </div>
      </div>
    </article>
  )
}
