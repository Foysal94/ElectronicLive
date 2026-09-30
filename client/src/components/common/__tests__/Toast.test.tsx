import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ToastProvider } from '../../../context/ToastProvider'
import { useToast } from '../../../hooks/useToast'
import { ToastContainer } from '../ToastContainer'



function TestToastConsumer() {
  const { showToast } = useToast()
  return (
    <div>
      <button
        type="button"
        onClick={() => showToast('Subscription confirmed!', 'success')}
      >
        Trigger Success Toast
      </button>
      <button
        type="button"
        onClick={() => showToast('Action failed', 'error')}
      >
        Trigger Error Toast
      </button>
      <ToastContainer />
    </div>
  )
}

describe('Toast Component & Context', () => {
  it('Should_throw_when_useToast_called_outside_provider', () => {
    function BadComponent() {
      useToast()
      return null
    }

    expect(() => render(<BadComponent />)).toThrow(
      'useToast must be used within a ToastProvider'
    )
  })

  it('Should_render_and_dismiss_toast_notifications', async () => {
    const user = userEvent.setup()

    render(
      <ToastProvider>
        <TestToastConsumer />
      </ToastProvider>
    )

    expect(screen.queryByRole('status')).not.toBeInTheDocument()

    const successBtn = screen.getByRole('button', { name: 'Trigger Success Toast' })
    await user.click(successBtn)

    const statusElement = screen.getByRole('status')
    expect(statusElement).toBeInTheDocument()
    expect(statusElement).toHaveTextContent('Subscription confirmed!')

    const dismissBtn = screen.getByRole('button', { name: 'Dismiss notification' })
    await user.click(dismissBtn)

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('Should_render_error_toast_correctly', async () => {
    const user = userEvent.setup()

    render(
      <ToastProvider>
        <TestToastConsumer />
      </ToastProvider>
    )

    const errorBtn = screen.getByRole('button', { name: 'Trigger Error Toast' })
    await user.click(errorBtn)

    const statusElement = screen.getByRole('status')
    expect(statusElement).toHaveTextContent('Action failed')
  })
})
