import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { EventResponse } from '../../../api/types'
import { mockMultiProviderEvent, mockSingleProviderEvent } from '../../../test/mocks/fixtures'
import { EventList } from '../EventList'
import { sortEventsChronologically } from '../utils'

describe('EventList Component', () => {
  it('Should_render_idle_prompt_when_isIdle_is_true', () => {
    render(
      <EventList
        events={[]}
        isIdle={true}
        isFetching={false}
        isError={false}
      />
    )

    expect(screen.getByText('Discover London Electronic Music')).toBeInTheDocument()
    expect(
      screen.getByText(/Select a London artist or club above/i)
    ).toBeInTheDocument()
  })

  it('Should_render_skeleton_state_while_fetching', () => {
    render(
      <EventList
        events={[]}
        isIdle={false}
        isFetching={true}
        isError={false}
      />
    )

    expect(screen.getByRole('status', { name: 'Loading upcoming events' })).toBeInTheDocument()
  })

  it('Should_render_empty_state_when_zero_results', () => {
    render(
      <EventList
        events={[]}
        isIdle={false}
        isFetching={false}
        isError={false}
        query="NonexistentArtist"
      />
    )

    expect(screen.getByText('No Gigs Found')).toBeInTheDocument()
    expect(
      screen.getByText(/No upcoming London gigs found for/i)
    ).toBeInTheDocument()
  })

  it('Should_render_error_banner_when_isError_is_true', () => {
    const handleRetry = vi.fn()
    render(
      <EventList
        events={[]}
        isIdle={false}
        isFetching={false}
        isError={true}
        error={new Error('Connection timed out')}
        onRetry={handleRetry}
      />
    )

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Connection timed out')).toBeInTheDocument()
  })

  it('Should_render_timetable_rows_chronologically', () => {
    const earlierEvent: EventResponse = {
      ...mockSingleProviderEvent,
      id: 'earlier-1',
      date: '2026-11-01',
      name: 'Earlier Event',
    }
    const laterEvent: EventResponse = {
      ...mockMultiProviderEvent,
      id: 'later-1',
      date: '2026-12-01',
      name: 'Later Event',
    }
    const undatedEvent: EventResponse = {
      ...mockSingleProviderEvent,
      id: 'undated-1',
      date: null,
      name: 'Undated Event',
    }

    // Pass in reverse order
    render(
      <EventList
        events={[undatedEvent, laterEvent, earlierEvent]}
        isIdle={false}
        isFetching={false}
        isError={false}
      />
    )

    expect(screen.getByText('Upcoming Shows (3)')).toBeInTheDocument()

    const sorted = sortEventsChronologically([undatedEvent, laterEvent, earlierEvent])
    expect(sorted[0]?.name).toBe('Earlier Event')
    expect(sorted[1]?.name).toBe('Later Event')
    expect(sorted[2]?.name).toBe('Undated Event')
  })

  it('Should_render_track_artist_button_in_header_when_query_is_present', async () => {
    const handleTrack = vi.fn()
    render(
      <EventList
        events={[mockSingleProviderEvent]}
        isIdle={false}
        isFetching={false}
        isError={false}
        query="Bicep"
        onTrackArtist={handleTrack}
      />
    )

    const trackBtn = screen.getByRole('button', { name: 'Track Bicep' })
    expect(trackBtn).toBeInTheDocument()
    trackBtn.click()
    expect(handleTrack).toHaveBeenCalledWith('Bicep')
  })

  it('Should_hide_track_artist_button_when_query_is_empty', () => {
    render(
      <EventList
        events={[mockSingleProviderEvent]}
        isIdle={false}
        isFetching={false}
        isError={false}
        query=""
        onTrackArtist={vi.fn()}
      />
    )

    expect(screen.queryByRole('button', { name: /track/i })).not.toBeInTheDocument()
  })
})


