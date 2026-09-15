import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DateBlock } from '../DateBlock'
import { parseDateOnly } from '../utils'

describe('DateBlock Component', () => {
  it('Should_parse_dates_without_timezone_day_shift', () => {
    // 2026-11-14 is a Saturday in UTC
    const parsed = parseDateOnly('2026-11-14')

    expect(parsed).toEqual({
      dayOfWeek: 'SAT',
      day: '14',
      month: 'NOV',
    })
  })

  it('Should_render_date_and_time_cleanly', () => {
    render(<DateBlock date="2026-11-14" time="14:00:00" />)

    expect(screen.getByText('SAT')).toBeInTheDocument()
    expect(screen.getByText('14')).toBeInTheDocument()
    expect(screen.getByText('NOV')).toBeInTheDocument()
    expect(screen.getByText('14:00')).toBeInTheDocument()
  })

  it('Should_render_tba_when_date_is_null', () => {
    render(<DateBlock date={null} />)

    expect(screen.getByText('TBA')).toBeInTheDocument()
    expect(screen.getByText('DATE')).toBeInTheDocument()
  })
})
