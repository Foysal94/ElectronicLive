import type { EventGenre } from '../../api/types'
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

export const QUICK_GENRES: readonly QuickGenreItem[] = [
  { label: 'Techno', value: 'techno' },
  { label: 'House', value: 'house' },
  { label: 'Drum & Bass', value: 'drum-and-bass' },
  { label: 'Trance', value: 'trance' },
  { label: 'Garage', value: 'garage' },
] as const
