import { describe, expect, it } from 'vitest'
import {
  DATE_PRESETS,
  formatDateOnly,
  getDaysDifference,
  matchActivePreset,
  parseDateOnly,
} from '../datePresets'

describe('datePresets Utility', () => {
  it('Should_format_dates_to_iso_yyyy_mm_dd_accurately', () => {
    const date = new Date(2026, 9, 5) // Oct 5, 2026
    expect(formatDateOnly(date)).toBe('2026-10-05')
  })

  it('Should_parse_valid_iso_dates_and_reject_invalid_calendar_dates', () => {
    expect(parseDateOnly('2026-10-05')).not.toBeNull()
    expect(parseDateOnly('2026-02-31')).toBeNull()
    expect(parseDateOnly('invalid')).toBeNull()
  })

  it('Should_calculate_days_difference_accurately', () => {
    expect(getDaysDifference('2026-10-01', '2026-10-08')).toBe(7)
    expect(getDaysDifference('2026-10-01', '2026-10-15')).toBe(14)
    expect(getDaysDifference('2026-10-10', '2026-10-05')).toBe(-5)
  })

  it('Should_compute_tonight_preset_as_current_day', () => {
    const monday = new Date(2026, 9, 5) // Monday Oct 5, 2026
    const tonight = DATE_PRESETS.find((p) => p.id === 'tonight')!
    const range = tonight.getRange(monday)

    expect(range.from).toBe('2026-10-05')
    expect(range.to).toBe('2026-10-05')
  })

  it('Should_compute_this_weekend_from_friday_to_sunday_when_current_day_is_monday', () => {
    const monday = new Date(2026, 9, 5) // Mon Oct 5, 2026
    const thisWeekend = DATE_PRESETS.find((p) => p.id === 'this-weekend')!
    const range = thisWeekend.getRange(monday)

    expect(range.from).toBe('2026-10-09') // Fri Oct 9
    expect(range.to).toBe('2026-10-11') // Sun Oct 11
  })

  it('Should_compute_this_weekend_from_friday_to_sunday_when_current_day_is_friday', () => {
    const friday = new Date(2026, 9, 9) // Fri Oct 9, 2026
    const thisWeekend = DATE_PRESETS.find((p) => p.id === 'this-weekend')!
    const range = thisWeekend.getRange(friday)

    expect(range.from).toBe('2026-10-09')
    expect(range.to).toBe('2026-10-11')
  })

  it('Should_compute_this_weekend_from_friday_to_sunday_when_current_day_is_saturday', () => {
    const saturday = new Date(2026, 9, 10) // Sat Oct 10, 2026
    const thisWeekend = DATE_PRESETS.find((p) => p.id === 'this-weekend')!
    const range = thisWeekend.getRange(saturday)

    expect(range.from).toBe('2026-10-09')
    expect(range.to).toBe('2026-10-11')
  })

  it('Should_compute_this_weekend_from_friday_to_sunday_when_current_day_is_sunday', () => {
    const sunday = new Date(2026, 9, 11) // Sun Oct 11, 2026
    const thisWeekend = DATE_PRESETS.find((p) => p.id === 'this-weekend')!
    const range = thisWeekend.getRange(sunday)

    expect(range.from).toBe('2026-10-09')
    expect(range.to).toBe('2026-10-11')
  })

  it('Should_compute_next_weekend_accurately', () => {
    const monday = new Date(2026, 9, 5) // Mon Oct 5, 2026
    const nextWeekend = DATE_PRESETS.find((p) => p.id === 'next-weekend')!
    const range = nextWeekend.getRange(monday)

    expect(range.from).toBe('2026-10-16') // Fri Oct 16
    expect(range.to).toBe('2026-10-18') // Sun Oct 18
  })

  it('Should_compute_next_30_days_range', () => {
    const monday = new Date(2026, 9, 5) // Mon Oct 5, 2026
    const next30 = DATE_PRESETS.find((p) => p.id === 'next-30-days')!
    const range = next30.getRange(monday)

    expect(range.from).toBe('2026-10-05')
    expect(range.to).toBe('2026-11-04')
  })

  it('Should_match_active_preset_when_dates_equal_preset_range', () => {
    const monday = new Date(2026, 9, 5)

    expect(matchActivePreset('2026-10-05', '2026-10-05', monday)).toBe('tonight')
    expect(matchActivePreset('2026-10-09', '2026-10-11', monday)).toBe('this-weekend')
    expect(matchActivePreset('2026-10-16', '2026-10-18', monday)).toBe('next-weekend')
    expect(matchActivePreset('2026-10-05', '2026-11-04', monday)).toBe('next-30-days')
    expect(matchActivePreset('2026-10-06', '2026-10-07', monday)).toBeNull()
  })
})
