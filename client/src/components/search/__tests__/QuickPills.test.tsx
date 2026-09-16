import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { QUICK_ARTISTS, QUICK_VENUES } from '../constants'
import { QuickPills } from '../QuickPills'

describe('QuickPills Component', () => {
  it('Should_render_artist_and_venue_pill_groups', () => {
    render(<QuickPills onSelect={vi.fn()} />)

    expect(screen.getByText('Quick Search Artists:')).toBeInTheDocument()
    expect(screen.getByText('Quick Search Venues:')).toBeInTheDocument()

    for (const artist of QUICK_ARTISTS) {
      expect(screen.getByRole('button', { name: artist })).toBeInTheDocument()
    }

    for (const venue of QUICK_VENUES) {
      expect(screen.getByRole('button', { name: venue })).toBeInTheDocument()
    }
  })

  it('Should_trigger_onSelect_when_pill_is_clicked', async () => {
    const user = userEvent.setup()
    const handleSelect = vi.fn()

    render(<QuickPills onSelect={handleSelect} />)

    const fabricPill = screen.getByRole('button', { name: 'Fabric' })
    await user.click(fabricPill)

    expect(handleSelect).toHaveBeenCalledTimes(1)
    expect(handleSelect).toHaveBeenCalledWith('Fabric')
  })

  it('Should_highlight_active_pill_matching_current_query', () => {
    render(<QuickPills activeQuery="Amelie Lens" onSelect={vi.fn()} />)

    const ameliePill = screen.getByRole('button', { name: 'Amelie Lens' })
    expect(ameliePill.className).toContain('text-emerald-400')
    expect(ameliePill.className).toContain('bg-emerald-950')
  })
})
