export type EventProvider = 'Ticketmaster' | 'Skiddle' | 'ResidentAdvisor'

export type EventStatus = 'OnSale' | 'SoldOut' | 'Postponed' | 'Cancelled' | 'Unknown'

export interface EventTicketOffer {
  provider: EventProvider
  ticketUrl: string | null
  status: EventStatus
}

export interface EventResponse {
  id: string
  name: string
  venueName: string
  date: string | null
  time: string | null
  ticketUrl: string | null
  status: EventStatus
  provider: EventProvider
  offers?: EventTicketOffer[] | null
}

const VALID_PROVIDERS: ReadonlySet<string> = new Set(['Ticketmaster', 'Skiddle', 'ResidentAdvisor'])
const VALID_STATUSES: ReadonlySet<string> = new Set(['OnSale', 'SoldOut', 'Postponed', 'Cancelled', 'Unknown'])

export function isEventProvider(value: unknown): value is EventProvider {
  return typeof value === 'string' && VALID_PROVIDERS.has(value)
}

export function isEventStatus(value: unknown): value is EventStatus {
  return typeof value === 'string' && VALID_STATUSES.has(value)
}

export function isEventTicketOffer(value: unknown): value is EventTicketOffer {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  return (
    'provider' in value &&
    isEventProvider(value.provider) &&
    'status' in value &&
    isEventStatus(value.status) &&
    'ticketUrl' in value &&
    (value.ticketUrl === null || typeof value.ticketUrl === 'string')
  )
}

export function isEventResponse(value: unknown): value is EventResponse {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  const hasValidOffers =
    !('offers' in value) ||
    value.offers === null ||
    value.offers === undefined ||
    (Array.isArray(value.offers) && value.offers.every(isEventTicketOffer))

  return (
    'id' in value &&
    typeof value.id === 'string' &&
    'name' in value &&
    typeof value.name === 'string' &&
    'venueName' in value &&
    typeof value.venueName === 'string' &&
    'date' in value &&
    (value.date === null || typeof value.date === 'string') &&
    'time' in value &&
    (value.time === null || typeof value.time === 'string') &&
    'ticketUrl' in value &&
    (value.ticketUrl === null || typeof value.ticketUrl === 'string') &&
    'status' in value &&
    isEventStatus(value.status) &&
    'provider' in value &&
    isEventProvider(value.provider) &&
    hasValidOffers
  )
}

export function isEventResponseList(value: unknown): value is EventResponse[] {
  return Array.isArray(value) && value.every(isEventResponse)
}
