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

    expect(screen.getByText('Bicep (DJ Set)')).toBeInTheDocument()
    expect(screen.getByText('Fabric')).toBeInTheDocument()
  })

  it('Should_render_event_with_two_providers', () => {
    render(<EventRow event={mockTwoProviderEvent} />)

    expect(screen.getByText('Charlotte de Witte - KNTXT London')).toBeInTheDocument()
    expect(screen.getByText('FOLD')).toBeInTheDocument()
    expect(screen.getByText('Resident Advisor')).toBeInTheDocument()
    expect(screen.getByText('Skiddle')).toBeInTheDocument()
  })

  it('Should_render_event_with_three_providers', () => {
    render(<EventRow event={mockMultiProviderEvent} />)

    expect(screen.getByText('Amelie Lens - Exhale London')).toBeInTheDocument()
    expect(screen.getByText('Drumsheds')).toBeInTheDocument()
    expect(screen.getByText('Resident Advisor')).toBeInTheDocument()
    expect(screen.getByText('Skiddle')).toBeInTheDocument()
    expect(screen.getByText('Ticketmaster')).toBeInTheDocument()
  })
})
