import {
  addDays,
  differenceInCalendarDays,
  format,
  getDay,
  isValid,
  parseISO,
  startOfDay,
} from 'date-fns'

export const DATE_PRESET_KEYS = {
  TONIGHT: 'tonight',
  THIS_WEEKEND: 'this-weekend',
  NEXT_WEEKEND: 'next-weekend',
  NEXT_30_DAYS: 'next-30-days',
} as const

export type DatePresetKey = (typeof DATE_PRESET_KEYS)[keyof typeof DATE_PRESET_KEYS]

export interface DateRange {
  from: string
  to: string
}

export interface DatePresetDefinition {
  id: DatePresetKey
  label: string
  getRange: (referenceDate?: Date) => DateRange
}

export const formatDateOnly = (d: Date): string => format(d, 'yyyy-MM-dd')

export const parseDateOnly = (s: string): Date | null =>
  /^\d{4}-\d{2}-\d{2}$/.test(s.trim()) && isValid(parseISO(s)) ? parseISO(s) : null

export const getDaysDifference = (from: string, to: string): number =>
  differenceInCalendarDays(parseISO(to), parseISO(from))

export function getThisWeekendRange(ref: Date = new Date()): DateRange {
  const today = startOfDay(ref)
  const day = getDay(today)
  const friday = addDays(today, day === 0 ? -2 : 5 - day)
  return {
    from: formatDateOnly(friday),
    to: formatDateOnly(addDays(friday, 2)),
  }
}

export function getNextWeekendRange(ref: Date = new Date()): DateRange {
  const { from } = getThisWeekendRange(ref)
  const nextFriday = addDays(parseISO(from), 7)
  return {
    from: formatDateOnly(nextFriday),
    to: formatDateOnly(addDays(nextFriday, 2)),
  }
}

export const DATE_PRESETS: DatePresetDefinition[] = [
  {
    id: DATE_PRESET_KEYS.TONIGHT,
    label: 'Tonight',
    getRange: (r = new Date()) => {
      const today = formatDateOnly(startOfDay(r))
      return { from: today, to: today }
    },
  },
  {
    id: DATE_PRESET_KEYS.THIS_WEEKEND,
    label: 'This Weekend',
    getRange: getThisWeekendRange,
  },
  {
    id: DATE_PRESET_KEYS.NEXT_WEEKEND,
    label: 'Next Weekend',
    getRange: getNextWeekendRange,
  },
  {
    id: DATE_PRESET_KEYS.NEXT_30_DAYS,
    label: 'Next 30 Days',
    getRange: (r = new Date()) => {
      const start = startOfDay(r)
      return {
        from: formatDateOnly(start),
        to: formatDateOnly(addDays(start, 30)),
      }
    },
  },
]

export const matchActivePreset = (
  from?: string,
  to?: string,
  ref: Date = new Date()
): DatePresetKey | null =>
  DATE_PRESETS.find((p) => {
    const range = p.getRange(ref)
    return range.from === from && range.to === to
  })?.id ?? null
