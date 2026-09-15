import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ErrorBanner, ErrorBoundary } from '../ErrorBoundary'

function ProblemChild({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) {
    throw new Error('Exploded in render')
  }
  return <div>Everything is normal</div>
}

describe('ErrorBoundary & ErrorBanner', () => {
  it('Should_render_children_when_no_error_occurs', () => {
    render(
      <ErrorBoundary>
        <div>Safe content</div>
      </ErrorBoundary>
    )

    expect(screen.getByText('Safe content')).toBeInTheDocument()
  })

  it('Should_catch_render_error_and_display_error_banner', () => {
    // Suppress console.error in JSDOM output for expected error throw
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

    render(
      <ErrorBoundary>
        <ProblemChild shouldThrow={true} />
      </ErrorBoundary>
    )

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Exploded in render')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try Again' })).toBeInTheDocument()

    consoleSpy.mockRestore()
  })

  it('Should_trigger_onRetry_when_try_again_button_is_clicked', async () => {
    const user = userEvent.setup()
    const handleRetry = vi.fn()

    render(
      <ErrorBanner
        title="Network Error"
        message="Failed to fetch data."
        onRetry={handleRetry}
      />
    )

    const retryButton = screen.getByRole('button', { name: 'Try Again' })
    await user.click(retryButton)

    expect(handleRetry).toHaveBeenCalledTimes(1)
  })
})
