import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { TrackArtistButton } from '../TrackArtistButton'

describe('TrackArtistButton Component', () => {
  it('Should_render_button_with_accessible_aria_label_and_artist_name', () => {
    render(<TrackArtistButton artistName="Bicep" onClick={vi.fn()} />)

    const button = screen.getByRole('button', { name: 'Track Bicep' })
    expect(button).toBeInTheDocument()
    expect(button).toHaveTextContent('Track Bicep')
    expect(button.className).toContain('min-h-[44px]')
  })

  it('Should_render_custom_label_when_provided', () => {
    render(
      <TrackArtistButton
        artistName="Overmono"
        label="Track Artist"
        onClick={vi.fn()}
      />
    )

    const button = screen.getByRole('button', { name: 'Track Overmono' })
    expect(button).toHaveTextContent('Track Artist')
  })

  it('Should_trigger_onClick_handler_when_clicked', async () => {
    const user = userEvent.setup()
    const handleClick = vi.fn()

    render(<TrackArtistButton artistName="Bicep" onClick={handleClick} />)

    const button = screen.getByRole('button', { name: 'Track Bicep' })
    await user.click(button)

    expect(handleClick).toHaveBeenCalledTimes(1)
  })
})
