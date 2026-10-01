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
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function parseDateOnly(dateString: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateString.trim())
  if (!match) return null

  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  const date = new Date(year, month - 1, day)

  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day
  ) {
    return null
  }

  return date
}

export function getDaysDifference(fromIso: string, toIso: string): number {
  const from = parseDateOnly(fromIso)
  const to = parseDateOnly(toIso)
  if (!from || !to) return 0

  const diffMs = to.getTime() - from.getTime()
  return Math.round(diffMs / (1000 * 60 * 60 * 24))
}

/**
 * Calculates Friday and Sunday of the current week.
 * Week starts Monday, with Sunday considered the end of the week.
 * Always yields Friday through Sunday of the current week regardless of today's day.
 */
function getThisWeekendRange(referenceDate: Date): DateRange {
  const dayOfWeek = referenceDate.getDay() // 0 = Sun, 1 = Mon, ..., 6 = Sat
  const fridayOffset = dayOfWeek === 0 ? -2 : 5 - dayOfWeek

  const friday = new Date(
    referenceDate.getFullYear(),
    referenceDate.getMonth(),
    referenceDate.getDate() + fridayOffset
  )

  const sunday = new Date(
    friday.getFullYear(),
    friday.getMonth(),
    friday.getDate() + 2
  )

  return {
    from: formatDateOnly(friday),
    to: formatDateOnly(sunday),
  }
}

function getNextWeekendRange(referenceDate: Date): DateRange {
  const thisWeekend = getThisWeekendRange(referenceDate)
  const thisFriday = parseDateOnly(thisWeekend.from) ?? referenceDate

  const nextFriday = new Date(
    thisFriday.getFullYear(),
    thisFriday.getMonth(),
    thisFriday.getDate() + 7
  )

  const nextSunday = new Date(
    nextFriday.getFullYear(),
    nextFriday.getMonth(),
    nextFriday.getDate() + 2
  )

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
      const today = formatDateOnly(ref)
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
      const toDate = new Date(ref.getFullYear(), ref.getMonth(), ref.getDate() + 30)
      return {
        from: formatDateOnly(ref),
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
