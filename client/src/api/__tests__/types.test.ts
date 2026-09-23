import { describe, expect, it } from 'vitest'
import {
  isEventGenre,
  isEventProvider,
  isEventResponse,
  isEventResponseList,
  isEventStatus,
  isEventTicketOffer,
} from '../types'

describe('Domain Type Guards', () => {
  it('Should_validate_valid_event_genres', () => {
    expect(isEventGenre('techno')).toBe(true)
    expect(isEventGenre('house')).toBe(true)
    expect(isEventGenre('drum-and-bass')).toBe(true)
    expect(isEventGenre('trance')).toBe(true)
    expect(isEventGenre('garage')).toBe(true)
  })

  it('Should_reject_invalid_event_genres', () => {
    expect(isEventGenre('rock')).toBe(false)
    expect(isEventGenre('Techno')).toBe(false)
    expect(isEventGenre('')).toBe(false)
    expect(isEventGenre(null)).toBe(false)
    expect(isEventGenre(undefined)).toBe(false)
  })

  it('Should_validate_valid_event_providers', () => {
    expect(isEventProvider('ResidentAdvisor')).toBe(true)
    expect(isEventProvider('Ticketmaster')).toBe(true)
    expect(isEventProvider('Skiddle')).toBe(true)
  })

  it('Should_reject_invalid_event_providers', () => {
    expect(isEventProvider('Dice')).toBe(false)
    expect(isEventProvider(123)).toBe(false)
    expect(isEventProvider(null)).toBe(false)
  })

  it('Should_validate_valid_event_statuses', () => {
    expect(isEventStatus('OnSale')).toBe(true)
    expect(isEventStatus('SoldOut')).toBe(true)
    expect(isEventStatus('Postponed')).toBe(true)
    expect(isEventStatus('Cancelled')).toBe(true)
    expect(isEventStatus('Unknown')).toBe(true)
  })

  it('Should_reject_invalid_event_statuses', () => {
    expect(isEventStatus('Available')).toBe(false)
    expect(isEventStatus('')).toBe(false)
    expect(isEventStatus(undefined)).toBe(false)
  })

  it('Should_validate_valid_event_ticket_offers', () => {
    const validOffer = {
      provider: 'ResidentAdvisor',
      ticketUrl: 'https://ra.co/events/1',
      status: 'OnSale',
    }
    expect(isEventTicketOffer(validOffer)).toBe(true)

    const validOfferWithNullUrl = {
      provider: 'Skiddle',
      ticketUrl: null,
      status: 'SoldOut',
    }
    expect(isEventTicketOffer(validOfferWithNullUrl)).toBe(true)
  })

  it('Should_reject_malformed_event_ticket_offers', () => {
    expect(isEventTicketOffer(null)).toBe(false)
    expect(isEventTicketOffer({ provider: 'Invalid', ticketUrl: null, status: 'OnSale' })).toBe(false)
    expect(isEventTicketOffer({ provider: 'ResidentAdvisor', ticketUrl: 123, status: 'OnSale' })).toBe(false)
  })

  it('Should_validate_valid_event_responses', () => {
    const validEvent = {
      id: 'event-1',
      name: 'Charlotte de Witte All Night Long',
      venueName: 'Drumsheds',
      date: '2026-11-14',
      time: '22:00:00',
      ticketUrl: 'https://ra.co/events/1',
      status: 'OnSale',
      provider: 'ResidentAdvisor',
      offers: [
        {
          provider: 'ResidentAdvisor',
          ticketUrl: 'https://ra.co/events/1',
          status: 'OnSale',
        },
      ],
    }
    expect(isEventResponse(validEvent)).toBe(true)
  })

  it('Should_reject_malformed_event_responses', () => {
    expect(isEventResponse(null)).toBe(false)
    expect(
      isEventResponse({
        id: 'event-1',
        name: 'Event',
        venueName: 'Venue',
        // missing date, time, etc.
      })
    ).toBe(false)
  })

  it('Should_validate_event_response_lists', () => {
    const validList = [
      {
        id: 'event-1',
        name: 'Event 1',
        venueName: 'Fabric',
        date: null,
        time: null,
        ticketUrl: null,
        status: 'Unknown',
        provider: 'Ticketmaster',
        offers: [],
      },
    ]
    expect(isEventResponseList(validList)).toBe(true)
    expect(isEventResponseList([1, 2, 3])).toBe(false)
    expect(isEventResponseList('not an array')).toBe(false)
  })
})
