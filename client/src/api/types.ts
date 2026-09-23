// Single source of truth for upstream gig providers
export const EVENT_PROVIDERS = ['Ticketmaster', 'Skiddle', 'ResidentAdvisor'] as const
export type EventProvider = (typeof EVENT_PROVIDERS)[number]

// Single source of truth for event ticketing statuses
export const EVENT_STATUSES = ['OnSale', 'SoldOut', 'Postponed', 'Cancelled', 'Unknown'] as const
export type EventStatus = (typeof EVENT_STATUSES)[number]

// Single source of truth for curated genre filters and UI labels
export const EVENT_GENRES = {
  TECHNO: { label: 'Techno', value: 'techno' },
  HOUSE: { label: 'House', value: 'house' },
  DRUM_AND_BASS: { label: 'Drum & Bass', value: 'drum-and-bass' },
  TRANCE: { label: 'Trance', value: 'trance' },
  GARAGE: { label: 'Garage', value: 'garage' },
} as const

export type EventGenre = (typeof EVENT_GENRES)[keyof typeof EVENT_GENRES]['value']
export const EVENT_GENRE_LIST = Object.values(EVENT_GENRES)

export interface EventSearchParams {
  query?: string
  genre?: EventGenre
  city?: string
}

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

const VALID_PROVIDERS: ReadonlySet<string> = new Set(EVENT_PROVIDERS)
const VALID_STATUSES: ReadonlySet<string> = new Set(EVENT_STATUSES)
const VALID_GENRES: ReadonlySet<string> = new Set(EVENT_GENRE_LIST.map((g) => g.value))

export function isEventGenre(value: unknown): value is EventGenre {
  return typeof value === 'string' && VALID_GENRES.has(value)
}

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
