import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { EmptyState } from '../EmptyState'

describe('EmptyState Component', () => {
  it('Should_render_idle_state_when_isIdle_is_true', () => {
    render(<EmptyState isIdle={true} />)

    expect(screen.getByText('Discover London Electronic Music')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /track/i })).not.toBeInTheDocument()
  })

  it('Should_render_no_gigs_found_message_with_query', () => {
    render(<EmptyState query="Jon Hopkins" />)

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.getByText('No Gigs Found')).toBeInTheDocument()
    expect(screen.getByText('"Jon Hopkins"')).toBeInTheDocument()
  })

  it('Should_render_track_artist_button_when_query_and_onTrackArtist_provided', () => {
    render(
      <EmptyState
        query="Jon Hopkins"
        onTrackArtist={vi.fn()}
      />
    )

    expect(screen.getByRole('button', { name: 'Track Jon Hopkins' })).toBeInTheDocument()
    expect(screen.getByText(/to get alerted when a show is announced/i)).toBeInTheDocument()
  })


  it('Should_trigger_onTrackArtist_callback_when_button_is_clicked', async () => {
    const user = userEvent.setup()
    const handleTrack = vi.fn()

    render(
      <EmptyState
        query="Overmono"
        onTrackArtist={handleTrack}
      />
    )

    const button = screen.getByRole('button', { name: 'Track Overmono' })
    await user.click(button)

    expect(handleTrack).toHaveBeenCalledWith('Overmono')
  })

  it('Should_not_render_track_button_when_query_is_empty', () => {
    render(
      <EmptyState
        query=""
        onTrackArtist={vi.fn()}
      />
    )

    expect(screen.queryByRole('button', { name: /track/i })).not.toBeInTheDocument()
  })
})

