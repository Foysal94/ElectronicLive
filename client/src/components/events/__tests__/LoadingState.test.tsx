import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { LoadingState } from '../LoadingState'

describe('LoadingState Component', () => {
  it('Should_render_live_feed_status_header_and_subtext', () => {
    render(<LoadingState />)

    expect(screen.getByRole('status', { name: 'Loading upcoming events' })).toBeInTheDocument()
    expect(screen.getByText('Connecting to live feeds...')).toBeInTheDocument()
    expect(
      screen.getByText('Aggregating shows from Resident Advisor, Ticketmaster & Skiddle')
    ).toBeInTheDocument()
  })

  it('Should_expose_accessible_live_region_and_status_role', () => {
    render(<LoadingState />)

    const statusRegion = screen.getByRole('status', { name: 'Loading upcoming events' })
    expect(statusRegion).toHaveAttribute('aria-live', 'polite')
  })
})
