import {
  addDays,
  differenceInCalendarDays,
  format,
  getDay,
  isValid,
  parseISO,
  startOfDay,
} from 'date-fns'

export type DatePresetKey = 'tonight' | 'this-weekend' | 'next-weekend' | 'next-30-days'

export interface DateRange {
  from: string
  to: string
}

export interface DatePresetDefinition {
  id: DatePresetKey
  label: string
  getRange: (referenceDate?: Date) => DateRange
}

export function formatDateOnly(date: Date): string {
  return format(date, 'yyyy-MM-dd')
}

export function parseDateOnly(dateString: string): Date | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(dateString.trim())) return null
  const parsed = parseISO(dateString)
  return isValid(parsed) ? parsed : null
}

export function normalizeReferenceDate(ref: Date = new Date()): Date {
  return startOfDay(ref)
}

export function getDaysDifference(fromIso: string, toIso: string): number {
  const from = parseDateOnly(fromIso)
  const to = parseDateOnly(toIso)
  if (!from || !to) return 0
  return differenceInCalendarDays(to, from)
}

function getThisWeekendRange(referenceDate: Date): DateRange {
  const normalized = normalizeReferenceDate(referenceDate)
  const dayOfWeek = getDay(normalized)
  const fridayOffset = dayOfWeek === 0 ? -2 : 5 - dayOfWeek

  const friday = addDays(normalized, fridayOffset)
  const sunday = addDays(friday, 2)

  return {
    from: formatDateOnly(friday),
    to: formatDateOnly(sunday),
  }
}

function getNextWeekendRange(referenceDate: Date): DateRange {
  const normalized = normalizeReferenceDate(referenceDate)
  const dayOfWeek = getDay(normalized)
  const fridayOffset = dayOfWeek === 0 ? -2 : 5 - dayOfWeek

  const thisFriday = addDays(normalized, fridayOffset)
  const nextFriday = addDays(thisFriday, 7)
  const nextSunday = addDays(nextFriday, 2)

  return {
    from: formatDateOnly(nextFriday),
    to: formatDateOnly(nextSunday),
  }
}

export const DATE_PRESETS: DatePresetDefinition[] = [
  {
    id: 'tonight',
    label: 'Tonight',
    getRange: (ref = new Date()) => {
      const today = formatDateOnly(normalizeReferenceDate(ref))
      return { from: today, to: today }
    },
  },
  {
    id: 'this-weekend',
    label: 'This Weekend',
    getRange: (ref = new Date()) => getThisWeekendRange(ref),
  },
  {
    id: 'next-weekend',
    label: 'Next Weekend',
    getRange: (ref = new Date()) => getNextWeekendRange(ref),
  },
  {
    id: 'next-30-days',
    label: 'Next 30 Days',
    getRange: (ref = new Date()) => {
      const normalized = normalizeReferenceDate(ref)
      const toDate = addDays(normalized, 30)
      return {
        from: formatDateOnly(normalized),
        to: formatDateOnly(toDate),
      }
    },
  },
]

export function matchActivePreset(
  activeFrom?: string,
  activeTo?: string,
  referenceDate: Date = new Date()
): DatePresetKey | null {
  if (!activeFrom || !activeTo) return null

  for (const preset of DATE_PRESETS) {
    const range = preset.getRange(referenceDate)
    if (range.from === activeFrom && range.to === activeTo) {
      return preset.id
    }
  }

  return null
}
