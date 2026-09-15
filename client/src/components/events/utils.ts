import type { EventResponse } from '../../api/types'

// Upstream C# DateOnly strings ("YYYY-MM-DD") evaluated via naive `new Date("YYYY-MM-DD")`
// default to UTC midnight, causing negative timezone offsets (e.g. UTC-5) to roll
// backward by one calendar day. We manually split components to construct UTC Date objects.
export function parseDateOnly(dateStr: string | null): {
  dayOfWeek: string
  day: string
  month: string
} | null {
  if (!dateStr || !dateStr.includes('-')) {
    return null
  }

  const parts = dateStr.split('-')
  if (parts.length !== 3) {
    return null
  }

  const year = parseInt(parts[0] ?? '', 10)
  const month = parseInt(parts[1] ?? '', 10) - 1
  const day = parseInt(parts[2] ?? '', 10)

  if (Number.isNaN(year) || Number.isNaN(month) || Number.isNaN(day)) {
    return null
  }

  const utc = new Date(Date.UTC(year, month, day))
  if (Number.isNaN(utc.getTime())) {
    return null
  }

  const dayOfWeek = utc
    .toLocaleDateString('en-GB', { weekday: 'short', timeZone: 'UTC' })
    .toUpperCase()
  const monthStr = utc
    .toLocaleDateString('en-GB', { month: 'short', timeZone: 'UTC' })
    .toUpperCase()

  return {
    dayOfWeek,
    day: String(day).padStart(2, '0'),
    month: monthStr,
  }
}

export function formatTime(timeStr?: string | null): string | null {
  if (!timeStr) return null
  const parts = timeStr.split(':')
  if (parts.length >= 2 && parts[0] && parts[1]) {
    return `${parts[0]}:${parts[1]}`
  }
  return timeStr
}

export function sortEventsChronologically(
  events: readonly EventResponse[]
): EventResponse[] {
  return [...events].sort((a, b) => {
    // Events without dates appear at the end
    if (!a.date && !b.date) return 0
    if (!a.date) return 1
    if (!b.date) return -1

    const dateDiff = a.date.localeCompare(b.date)
    if (dateDiff !== 0) return dateDiff

    // Compare time if same date
    if (!a.time && !b.time) return 0
    if (!a.time) return 1
    if (!b.time) return -1

    return a.time.localeCompare(b.time)
  })
}
