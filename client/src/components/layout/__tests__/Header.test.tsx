import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Header } from '../Header'

describe('Header Component', () => {
  it('Should_render_header_with_branding_and_attribution', () => {
    render(<Header />)

    expect(screen.getByRole('banner')).toBeInTheDocument()
    expect(screen.getByText('ElectronicLive')).toBeInTheDocument()
    expect(
      screen.getByText(/Aggregating live events from/i)
    ).toBeInTheDocument()
    expect(screen.getByText('Resident Advisor')).toBeInTheDocument()
    expect(screen.getByText('Ticketmaster')).toBeInTheDocument()
    expect(screen.getByText('Skiddle')).toBeInTheDocument()
  })

  it('Should_render_london_location_scope_badge', () => {
    render(<Header />)

    const badge = screen.getByLabelText('City scope: London, UK')
    expect(badge).toBeInTheDocument()
    expect(badge).toHaveTextContent('📍 London, UK')
  })
})
