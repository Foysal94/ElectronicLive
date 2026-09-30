import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { SubscriptionForm } from '../SubscriptionForm'

describe('SubscriptionForm Component', () => {
  it('Should_render_form_fields_and_accessible_inputs', () => {
    render(
      <SubscriptionForm
        email="test@example.com"
        onChangeEmail={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
        isPending={false}
        hasError={false}
      />
    )

    const input = screen.getByLabelText(/email address/i)
    expect(input).toBeInTheDocument()
    expect(input).toHaveValue('test@example.com')
    expect(screen.getByRole('button', { name: /^subscribe$/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^cancel$/i })).toBeInTheDocument()
  })

  it('Should_trigger_onChangeEmail_when_user_types', async () => {
    const user = userEvent.setup()
    const handleChange = vi.fn()

    render(
      <SubscriptionForm
        email=""
        onChangeEmail={handleChange}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
        isPending={false}
        hasError={false}
      />
    )

    const input = screen.getByLabelText(/email address/i)
    await user.type(input, 'a')

    expect(handleChange).toHaveBeenCalledWith('a')
  })

  it('Should_disable_inputs_and_show_spinner_when_isPending', () => {
    render(
      <SubscriptionForm
        email="test@example.com"
        onChangeEmail={vi.fn()}
        onSubmit={vi.fn()}
        onCancel={vi.fn()}
        isPending={true}
        hasError={false}
      />
    )

    expect(screen.getByLabelText(/email address/i)).toBeDisabled()
    expect(screen.getByRole('button', { name: /subscribing.../i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /^cancel$/i })).toBeDisabled()
  })
})
