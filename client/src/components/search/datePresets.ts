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
  const year = date.getUTCFullYear()
  const month = String(date.getUTCMonth() + 1).padStart(2, '0')
  const day = String(date.getUTCDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function parseDateOnly(dateString: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateString.trim())
  if (!match) return null

  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  const date = new Date(Date.UTC(year, month - 1, day))

  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
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

function getThisWeekendRange(referenceDate: Date): DateRange {
  const dayOfWeek = referenceDate.getUTCDay()
  const fridayOffset = dayOfWeek === 0 ? -2 : 5 - dayOfWeek

  const friday = new Date(
    Date.UTC(
      referenceDate.getUTCFullYear(),
      referenceDate.getUTCMonth(),
      referenceDate.getUTCDate() + fridayOffset
    )
  )

  const sunday = new Date(
    Date.UTC(
      friday.getUTCFullYear(),
      friday.getUTCMonth(),
      friday.getUTCDate() + 2
    )
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
    Date.UTC(
      thisFriday.getUTCFullYear(),
      thisFriday.getUTCMonth(),
      thisFriday.getUTCDate() + 7
    )
  )

  const nextSunday = new Date(
    Date.UTC(
      nextFriday.getUTCFullYear(),
      nextFriday.getUTCMonth(),
      nextFriday.getUTCDate() + 2
    )
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
      const toDate = new Date(
        Date.UTC(ref.getUTCFullYear(), ref.getUTCMonth(), ref.getUTCDate() + 30)
      )
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
