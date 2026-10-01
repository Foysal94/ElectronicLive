import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DateFilterBar } from '../DateFilterBar'
import { DATE_PRESETS } from '../datePresets'

describe('DateFilterBar Component', () => {
  const fixedNow = new Date(2026, 9, 5, 12, 0, 0) // Monday Oct 5, 2026

  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(fixedNow)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('Should_render_all_date_preset_buttons_and_custom_trigger', () => {
    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={() => {}}
        hasSearchContext={true}
      />
    )

    expect(screen.getByRole('button', { name: 'Tonight' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'This Weekend' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next Weekend' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next 30 Days' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Custom...' })).toBeInTheDocument()
  })

  it('Should_highlight_active_preset_pill_with_emerald_styling', () => {
    const thisWeekend = DATE_PRESETS.find((p) => p.id === 'this-weekend')!.getRange(fixedNow)

    render(
      <DateFilterBar
        activeFrom={thisWeekend.from}
        activeTo={thisWeekend.to}
        onSelectDateRange={() => {}}
        hasSearchContext={true}
      />
    )

    const thisWeekendBtn = screen.getByRole('button', { name: 'This Weekend' })
    expect(thisWeekendBtn).toHaveClass('bg-emerald-950', 'text-emerald-400', 'border-emerald-800')

    const tonightBtn = screen.getByRole('button', { name: 'Tonight' })
    expect(tonightBtn).not.toHaveClass('bg-emerald-950')
  })

  it('Should_not_apply_active_emerald_styling_to_custom_button_when_merely_expanded_while_preset_is_active', async () => {
    vi.useRealTimers()
    const tonightRange = DATE_PRESETS.find((p) => p.id === 'tonight')!.getRange()

    render(
      <DateFilterBar
        activeFrom={tonightRange.from}
        activeTo={tonightRange.to}
        onSelectDateRange={() => {}}
        hasSearchContext={true}
      />
    )

    const user = userEvent.setup()
    const customBtn = screen.getByRole('button', { name: 'Custom...' })
    await user.click(customBtn)

    expect(customBtn).not.toHaveClass('bg-emerald-950')
    const tonightBtn = screen.getByRole('button', { name: 'Tonight' })
    expect(tonightBtn).toHaveClass('bg-emerald-950')
  })

  it('Should_call_onSelectDateRange_with_calculated_bounds_when_preset_clicked', async () => {
    vi.useRealTimers()
    const onSelect = vi.fn()

    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={onSelect}
        hasSearchContext={true}
      />
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Tonight' }))

    expect(onSelect).toHaveBeenCalledTimes(1)
    const [fromArg, toArg] = onSelect.mock.calls[0]
    expect(fromArg).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(toArg).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(fromArg).toBe(toArg)
  })

  it('Should_toggle_off_and_clear_dates_when_active_preset_is_clicked', async () => {
    vi.useRealTimers()
    const onSelect = vi.fn()
    const tonightRange = DATE_PRESETS.find((p) => p.id === 'tonight')!.getRange()

    render(
      <DateFilterBar
        activeFrom={tonightRange.from}
        activeTo={tonightRange.to}
        onSelectDateRange={onSelect}
        hasSearchContext={true}
      />
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Tonight' }))

    expect(onSelect).toHaveBeenCalledWith('', '')
  })

  it('Should_disable_next_30_days_preset_when_no_query_or_genre_context_exists', () => {
    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={() => {}}
        hasSearchContext={false}
      />
    )

    const next30Btn = screen.getByRole('button', { name: /next 30 days/i })
    expect(next30Btn).toBeDisabled()
    expect(next30Btn).toHaveAttribute(
      'title',
      'Artist or genre required for date ranges exceeding 7 days'
    )
  })

  it('Should_enable_next_30_days_preset_when_query_or_genre_context_exists', () => {
    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={() => {}}
        hasSearchContext={true}
      />
    )

    const next30Btn = screen.getByRole('button', { name: /next 30 days/i })
    expect(next30Btn).not.toBeDisabled()
  })

  it('Should_expand_custom_tray_when_custom_button_clicked', async () => {
    vi.useRealTimers()
    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={() => {}}
        hasSearchContext={true}
      />
    )

    expect(screen.queryByLabelText(/choose date/i)).not.toBeInTheDocument()

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))

    expect(screen.getByLabelText(/choose date/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Apply' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
  })

  it('Should_apply_custom_date_range_when_valid_dates_submitted', async () => {
    vi.useRealTimers()
    const onSelect = vi.fn()

    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={onSelect}
        hasSearchContext={true}
      />
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))

    const applyButton = screen.getByRole('button', { name: 'Apply' })
    expect(applyButton).toBeDisabled()

    const day5 = screen.getByRole('gridcell', { name: /October 5th,/i })
    const day15 = screen.getByRole('gridcell', { name: /October 15th,/i })
    await user.click(day5)
    await user.click(day15)

    expect(applyButton).toBeEnabled()
    await user.click(applyButton)

    expect(onSelect).toHaveBeenCalledWith('2026-10-05', '2026-10-15')
    expect(screen.queryByLabelText(/choose date/i)).not.toBeInTheDocument()
  })

  it('Should_reset_start_date_when_earlier_date_is_clicked', async () => {
    vi.useRealTimers()
    const onSelect = vi.fn()

    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={onSelect}
        hasSearchContext={true}
      />
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))

    const day15 = screen.getByRole('gridcell', { name: /October 15th,/i })
    const day5 = screen.getByRole('gridcell', { name: /October 5th,/i })
    await user.click(day15)
    await user.click(day5)
    await user.click(day15)

    const applyButton = screen.getByRole('button', { name: 'Apply' })
    expect(applyButton).toBeEnabled()
    await user.click(applyButton)

    expect(onSelect).toHaveBeenCalledWith('2026-10-05', '2026-10-15')
  })

  it('Should_disable_apply_button_if_range_exceeds_7_days_without_search_context', async () => {
    vi.useRealTimers()
    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={() => {}}
        hasSearchContext={false}
      />
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))

    const day1 = screen.getByRole('gridcell', { name: /October 1st,/i })
    const day20 = screen.getByRole('gridcell', { name: /October 20th,/i })
    await user.click(day1)
    await user.click(day20)

    const applyButton = screen.getByRole('button', { name: 'Apply' })
    expect(applyButton).toBeDisabled()
    expect(screen.getByText(/exceeds 7-day limit/i)).toBeInTheDocument()
  })

  it('Should_close_custom_tray_when_cancel_button_clicked_without_selecting_dates', async () => {
    vi.useRealTimers()
    const onSelect = vi.fn()

    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={onSelect}
        hasSearchContext={true}
      />
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))
    expect(screen.getByLabelText(/choose date/i)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByLabelText(/choose date/i)).not.toBeInTheDocument()
    expect(onSelect).not.toHaveBeenCalled()
  })

  it('Should_enforce_minimum_44px_tap_target_height_constraint', async () => {
    vi.useRealTimers()
    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={() => {}}
        hasSearchContext={true}
      />
    )

    const buttons = screen.getAllByRole('button')
    for (const btn of buttons) {
      expect(btn).toHaveClass('min-h-[44px]')
    }

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))

    const actionButtons = [
      screen.getByRole('button', { name: 'Apply' }),
      screen.getByRole('button', { name: 'Cancel' }),
    ]
    for (const btn of actionButtons) {
      expect(btn).toHaveClass('min-h-[44px]')
    }
  })

  it('Should_close_custom_tray_when_clicking_outside', async () => {
    vi.useRealTimers()
    render(
      <div>
        <div data-testid="outside">Outside area</div>
        <DateFilterBar
          activeFrom=""
          activeTo=""
          onSelectDateRange={() => {}}
          hasSearchContext={true}
        />
      </div>
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))
    expect(screen.getByLabelText(/choose date/i)).toBeInTheDocument()

    await user.click(screen.getByTestId('outside'))
    expect(screen.queryByLabelText(/choose date/i)).not.toBeInTheDocument()
  })

  it('Should_close_custom_tray_when_pressing_Escape', async () => {
    vi.useRealTimers()
    render(
      <DateFilterBar
        activeFrom=""
        activeTo=""
        onSelectDateRange={() => {}}
        hasSearchContext={true}
      />
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button', { name: 'Custom...' }))
    expect(screen.getByLabelText(/choose date/i)).toBeInTheDocument()

    await user.keyboard('{Escape}')
    expect(screen.queryByLabelText(/choose date/i)).not.toBeInTheDocument()
  })
})
