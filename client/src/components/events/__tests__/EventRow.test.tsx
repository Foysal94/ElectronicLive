import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import {
  mockMultiProviderEvent,
  mockSingleProviderEvent,
  mockTwoProviderEvent,
} from '../../../test/mocks/fixtures'
import { EventRow } from '../EventRow'

describe('EventRow Component', () => {
  it('Should_render_event_with_single_provider', () => {
    render(<EventRow event={mockSingleProviderEvent} />)

    expect(screen.getAllByText('Bicep (DJ Set)').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Fabric').length).toBeGreaterThan(0)
  })

  it('Should_render_event_with_two_providers', () => {
    render(<EventRow event={mockTwoProviderEvent} />)

    expect(screen.getAllByText('Charlotte de Witte - KNTXT London').length).toBeGreaterThan(0)
    expect(screen.getAllByText('FOLD').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Resident Advisor').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Skiddle').length).toBeGreaterThan(0)
  })

  it('Should_render_event_with_three_providers', () => {
    render(<EventRow event={mockMultiProviderEvent} />)

    expect(screen.getAllByText('Amelie Lens - Exhale London').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Drumsheds').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Resident Advisor').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Skiddle').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Ticketmaster').length).toBeGreaterThan(0)
  })
})
