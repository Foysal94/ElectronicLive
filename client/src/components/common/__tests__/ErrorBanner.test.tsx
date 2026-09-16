import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ErrorBanner } from '../ErrorBanner'

describe('ErrorBanner Component', () => {
  it('Should_render_default_title_and_message', () => {
    render(<ErrorBanner />)

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Service Connection Error')).toBeInTheDocument()
    expect(screen.getByText(/Unable to reach the London events service/i)).toBeInTheDocument()
  })

  it('Should_render_custom_title_and_message', () => {
    render(<ErrorBanner title="Custom Title" message="Custom error message." />)

    expect(screen.getByText('Custom Title')).toBeInTheDocument()
    expect(screen.getByText('Custom error message.')).toBeInTheDocument()
  })

  it('Should_trigger_onRetry_when_try_again_button_is_clicked', async () => {
    const user = userEvent.setup()
    const handleRetry = vi.fn()

    render(<ErrorBanner onRetry={handleRetry} />)

    const retryButton = screen.getByRole('button', { name: 'Try Again' })
    await user.click(retryButton)

    expect(handleRetry).toHaveBeenCalledTimes(1)
  })
})
