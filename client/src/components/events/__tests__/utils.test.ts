import { describe, expect, it } from 'vitest'
import type { EventResponse } from '../../../api/types'
import { formatTime, parseDateOnly, sortEventsChronologically } from '../utils'

describe('Events utils', () => {
  describe('parseDateOnly', () => {
    it('Should_parse_valid_date_into_utc_components', () => {
      const result = parseDateOnly('2026-11-14')

      expect(result).toEqual({
        dayOfWeek: 'SAT',
        day: '14',
        month: 'NOV',
      })
    })

    it('Should_pad_single_digit_days_with_zero', () => {
      const result = parseDateOnly('2026-01-05')

      expect(result).toEqual({
        dayOfWeek: 'MON',
        day: '05',
        month: 'JAN',
      })
    })

    it('Should_return_null_for_null_or_empty_input', () => {
      expect(parseDateOnly(null)).toBeNull()
      expect(parseDateOnly('')).toBeNull()
    })

    it('Should_return_null_for_non_dashed_or_malformed_formats', () => {
      expect(parseDateOnly('20261114')).toBeNull()
      expect(parseDateOnly('2026-11')).toBeNull()
      expect(parseDateOnly('2026/11/14')).toBeNull()
      expect(parseDateOnly('year-month-day')).toBeNull()
    })
  })

  describe('formatTime', () => {
    it('Should_truncate_seconds_from_valid_time_string', () => {
      expect(formatTime('22:00:00')).toBe('22:00')
      expect(formatTime('09:30:15')).toBe('09:30')
    })

    it('Should_preserve_already_formatted_hh_mm_time', () => {
      expect(formatTime('22:00')).toBe('22:00')
    })

    it('Should_return_null_for_falsy_or_null_inputs', () => {
      expect(formatTime(null)).toBeNull()
      expect(formatTime(undefined)).toBeNull()
      expect(formatTime('')).toBeNull()
    })

    it('Should_return_original_string_when_unparseable_without_colon', () => {
      expect(formatTime('TBD')).toBe('TBD')
    })
  })

  describe('sortEventsChronologically', () => {
    const createMockEvent = (
      id: string,
      date: string | null,
      time: string | null
    ): EventResponse => ({
      id,
      name: `Event ${id}`,
      venueName: 'Venue',
      date,
      time,
      ticketUrl: 'https://example.com',
      status: 'OnSale',
      provider: 'Ticketmaster',
    })

    it('Should_sort_events_in_ascending_order_by_date', () => {
      const e1 = createMockEvent('1', '2026-11-20', '22:00:00')
      const e2 = createMockEvent('2', '2026-11-10', '20:00:00')
      const e3 = createMockEvent('3', '2026-12-01', '21:00:00')

      const sorted = sortEventsChronologically([e1, e2, e3])

      expect(sorted.map((e) => e.id)).toEqual(['2', '1', '3'])
    })

    it('Should_sort_by_time_when_dates_are_identical', () => {
      const e1 = createMockEvent('1', '2026-11-14', '23:00:00')
      const e2 = createMockEvent('2', '2026-11-14', '19:00:00')
      const e3 = createMockEvent('3', '2026-11-14', '21:30:00')

      const sorted = sortEventsChronologically([e1, e2, e3])

      expect(sorted.map((e) => e.id)).toEqual(['2', '3', '1'])
    })

    it('Should_place_events_with_time_before_events_without_time_on_same_date', () => {
      const noTime = createMockEvent('1', '2026-11-14', null)
      const withTime = createMockEvent('2', '2026-11-14', '20:00:00')

      const sorted = sortEventsChronologically([noTime, withTime])

      expect(sorted.map((e) => e.id)).toEqual(['2', '1'])
    })

    it('Should_place_events_without_dates_at_the_end', () => {
      const dated1 = createMockEvent('1', '2026-11-14', '20:00:00')
      const undated = createMockEvent('2', null, null)
      const dated2 = createMockEvent('3', '2026-11-10', '19:00:00')

      const sorted = sortEventsChronologically([dated1, undated, dated2])

      expect(sorted.map((e) => e.id)).toEqual(['3', '1', '2'])
    })

    it('Should_not_mutate_the_original_array', () => {
      const original = [
        createMockEvent('1', '2026-11-20', null),
        createMockEvent('2', '2026-11-10', null),
      ]
      const copy = [...original]

      sortEventsChronologically(original)

      expect(original).toEqual(copy)
    })
  })
})
