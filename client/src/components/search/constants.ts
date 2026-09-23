import { EVENT_GENRE_LIST, type EventGenre } from '../../api/types'

export const QUICK_ARTISTS = [
  'Hardwell',
  'Armin van Buuren',
  'Amelie Lens',
  'Charlotte de Witte',
  'Bicep',
  'Eric Prydz',
] as const

export const QUICK_VENUES = [
  'Drumsheds',
  'Fabric',
  'FOLD',
  'Ministry of Sound',
  'Studio 338',
] as const

export interface QuickGenreItem {
  readonly label: string
  readonly value: EventGenre
}

// Curated electronic music genres derived directly from the API type definition
export const QUICK_GENRES: readonly QuickGenreItem[] = EVENT_GENRE_LIST
