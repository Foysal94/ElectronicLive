import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LoadingState } from '../LoadingState'

describe('LoadingState Component', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('Should_render_live_feed_status_header_and_subtext', () => {
    render(<LoadingState />)

    expect(screen.getByRole('status', { name: 'Loading upcoming events' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 3, name: 'Connecting to live feeds...' })).toBeInTheDocument()
    expect(
      screen.getByText('Aggregating shows from Resident Advisor, Ticketmaster & Skiddle')
    ).toBeInTheDocument()
  })

  it('Should_expose_accessible_live_region_and_status_role', () => {
    render(<LoadingState />)

    const statusRegion = screen.getByRole('status', { name: 'Loading upcoming events' })
    expect(statusRegion).toHaveAttribute('aria-live', 'polite')
  })

  it('Should_transition_to_on_demand_spin_up_notice_when_delayed_beyond_threshold', () => {
    vi.useFakeTimers()
    render(<LoadingState />)

    // Initially in standard feed aggregation state
    expect(screen.getByRole('heading', { level: 3, name: 'Connecting to live feeds...' })).toBeInTheDocument()
    expect(
      screen.getByText('Aggregating shows from Resident Advisor, Ticketmaster & Skiddle')
    ).toBeInTheDocument()

    // Advance by default threshold (4000ms)
    act(() => {
      vi.advanceTimersByTime(4000)
    })

    // Transitions to on-demand spin up explanation
    expect(
      screen.getByRole('heading', { level: 3, name: 'Starting backend services on demand (~10s)...' })
    ).toBeInTheDocument()
    expect(
      screen.getByText('Gathering data across Resident Advisor, Skiddle & Ticketmaster.')
    ).toBeInTheDocument()
  })

  it('Should_clear_timer_on_unmount_without_state_errors', () => {
    vi.useFakeTimers()
    const { unmount } = render(<LoadingState />)

    unmount()

    // Advancing past threshold after unmount should not throw or trigger state updates
    expect(() => {
      act(() => {
        vi.advanceTimersByTime(5000)
      })
    }).not.toThrow()
  })
})
