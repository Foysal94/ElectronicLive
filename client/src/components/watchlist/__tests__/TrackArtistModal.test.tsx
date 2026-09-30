import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { createQueryWrapper } from '../../../test/utils'
import { TrackArtistModal } from '../TrackArtistModal'

describe('TrackArtistModal Component', () => {
  it('Should_not_render_when_isOpen_is_false', () => {
    render(
      <TrackArtistModal
        isOpen={false}
        artistName="Bicep"
        onClose={vi.fn()}
      />,
      { wrapper: createQueryWrapper() }
    )

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('Should_render_dialog_with_artist_title_and_form_when_open', () => {
    render(
      <TrackArtistModal
        isOpen={true}
        artistName="Bicep"
        onClose={vi.fn()}
      />,
      { wrapper: createQueryWrapper() }
    )

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /track bicep/i })).toBeInTheDocument()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^subscribe$/i })).toBeInTheDocument()
  })

  it('Should_show_validation_error_when_submitting_invalid_email', async () => {
    const user = userEvent.setup()

    render(
      <TrackArtistModal
        isOpen={true}
        artistName="Bicep"
        onClose={vi.fn()}
      />,
      { wrapper: createQueryWrapper() }
    )

    const input = screen.getByLabelText(/email address/i)
    const submitBtn = screen.getByRole('button', { name: /^subscribe$/i })

    // Test empty submission
    await user.click(submitBtn)
    expect(screen.getByText('A valid email address is required.')).toBeInTheDocument()

    // Test malformed email
    await user.type(input, 'notanemail')
    await user.click(submitBtn)
    expect(screen.getByText('Please enter a valid email address.')).toBeInTheDocument()
  })

  it('Should_show_success_view_on_submission_and_close_when_done_clicked', async () => {
    const user = userEvent.setup()
    const handleClose = vi.fn()
    const handleSubscribed = vi.fn()

    render(
      <TrackArtistModal
        isOpen={true}
        artistName="Bicep"
        onClose={handleClose}
        onSubscribed={handleSubscribed}
      />,
      { wrapper: createQueryWrapper() }
    )

    const input = screen.getByLabelText(/email address/i)
    const submitBtn = screen.getByRole('button', { name: /^subscribe$/i })

    await user.type(input, 'valid@example.com')
    await user.click(submitBtn)

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 3, name: "You're tracking Bicep!" })).toBeInTheDocument()
    })

    expect(screen.getByText(/valid@example\.com/)).toBeInTheDocument()
    expect(handleSubscribed).toHaveBeenCalledWith(
      expect.objectContaining({
        subscriptionId: '123e4567-e89b-12d3-a456-426614174000',
        message: 'Subscribed successfully',
      })
    )
    expect(handleClose).not.toHaveBeenCalled()

    const doneBtn = screen.getByRole('button', { name: /^done$/i })
    await user.click(doneBtn)

    expect(handleClose).toHaveBeenCalledTimes(1)
  })

  it('Should_display_api_error_banner_when_submission_fails', async () => {
    const user = userEvent.setup()

    render(
      <TrackArtistModal
        isOpen={true}
        artistName="UnknownArtist"
        onClose={vi.fn()}
      />,
      { wrapper: createQueryWrapper() }
    )

    const input = screen.getByLabelText(/email address/i)
    const submitBtn = screen.getByRole('button', { name: /^subscribe$/i })

    await user.type(input, 'user@example.com')
    await user.click(submitBtn)

    await waitFor(() => {
      expect(
        screen.getByText("Artist 'UnknownArtist' could not be verified as a genuine music entity.")
      ).toBeInTheDocument()
    })
  })

  it('Should_close_modal_when_clicking_close_or_cancel_button', async () => {
    const user = userEvent.setup()
    const handleClose = vi.fn()

    const { rerender } = render(
      <TrackArtistModal
        isOpen={true}
        artistName="Bicep"
        onClose={handleClose}
      />,
      { wrapper: createQueryWrapper() }
    )

    const closeBtn = screen.getByRole('button', { name: /close modal/i })
    await user.click(closeBtn)
    expect(handleClose).toHaveBeenCalledTimes(1)

    rerender(
      <TrackArtistModal
        isOpen={true}
        artistName="Bicep"
        onClose={handleClose}
      />
    )

    const cancelBtn = screen.getByRole('button', { name: /^cancel$/i })
    await user.click(cancelBtn)
    expect(handleClose).toHaveBeenCalledTimes(2)
  })

  it('Should_close_modal_when_pressing_escape_key', async () => {
    const user = userEvent.setup()
    const handleClose = vi.fn()

    render(
      <TrackArtistModal
        isOpen={true}
        artistName="Bicep"
        onClose={handleClose}
      />,
      { wrapper: createQueryWrapper() }
    )

    await user.keyboard('{Escape}')
    expect(handleClose).toHaveBeenCalledTimes(1)
  })
})
