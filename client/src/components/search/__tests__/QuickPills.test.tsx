import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { QUICK_ARTISTS, QUICK_GENRES, QUICK_VENUES } from '../constants'
import { QuickPills } from '../QuickPills'

describe('QuickPills Component', () => {
  it('Should_render_artist_venue_and_genre_pill_groups', () => {
    render(<QuickPills onSelect={vi.fn()} />)

    expect(screen.getByText('Quick Search Artists:')).toBeInTheDocument()
    expect(screen.getByText('Quick Search Venues:')).toBeInTheDocument()
    expect(screen.getByText('Quick Search Genres:')).toBeInTheDocument()

    for (const artist of QUICK_ARTISTS) {
      expect(screen.getByRole('button', { name: artist })).toBeInTheDocument()
    }

    for (const venue of QUICK_VENUES) {
      expect(screen.getByRole('button', { name: venue })).toBeInTheDocument()
    }

    for (const genre of QUICK_GENRES) {
      expect(screen.getByRole('button', { name: genre.label })).toBeInTheDocument()
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

  it('Should_trigger_onSelectGenre_when_genre_pill_clicked', async () => {
    const user = userEvent.setup()
    const handleSelectGenre = vi.fn()

    render(<QuickPills onSelectGenre={handleSelectGenre} />)

    const technoPill = screen.getByRole('button', { name: 'Techno' })
    await user.click(technoPill)

    expect(handleSelectGenre).toHaveBeenCalledTimes(1)
    expect(handleSelectGenre).toHaveBeenCalledWith('techno')
  })

  it('Should_highlight_active_pill_matching_current_query', () => {
    render(<QuickPills activeQuery="Amelie Lens" onSelect={vi.fn()} />)

    const ameliePill = screen.getByRole('button', { name: 'Amelie Lens' })
    expect(ameliePill.className).toContain('text-emerald-400')
    expect(ameliePill.className).toContain('bg-emerald-950')
  })

  it('Should_highlight_active_genre_pill', () => {
    render(<QuickPills activeGenre="drum-and-bass" onSelectGenre={vi.fn()} />)

    const dnbPill = screen.getByRole('button', { name: 'Drum & Bass' })
    expect(dnbPill.className).toContain('text-emerald-400')
    expect(dnbPill.className).toContain('bg-emerald-950')
  })
})

