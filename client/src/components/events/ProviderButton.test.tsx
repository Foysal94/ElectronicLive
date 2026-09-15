import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ProviderButton } from './ProviderButton'

describe('ProviderButton Component', () => {
  it('Should_render_provider_outbound_ticket_link_with_badge', () => {
    render(
      <ProviderButton
        provider="ResidentAdvisor"
        ticketUrl="https://ra.co/events/1001"
        status="OnSale"
      />
    )

    const link = screen.getByRole('link', {
      name: /Get tickets on Resident Advisor \(OnSale\)/i,
    })
    expect(link).toBeInTheDocument()
    expect(link).toHaveAttribute('href', 'https://ra.co/events/1001')
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    expect(screen.getByText('On Sale')).toBeInTheDocument()
  })

  it('Should_render_disabled_button_when_ticket_url_is_null', () => {
    render(
      <ProviderButton
        provider="Ticketmaster"
        ticketUrl={null}
        status="OnSale"
      />
    )

    const button = screen.getByRole('button', {
      name: /Ticketmaster: Tickets TBA/i,
    })
    expect(button).toBeInTheDocument()
    expect(button).toBeDisabled()
    expect(screen.getByText('TBA')).toBeInTheDocument()
  })
})
