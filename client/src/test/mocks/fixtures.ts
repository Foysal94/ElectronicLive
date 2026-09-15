import type { EventResponse } from '../../api/types'

export const mockMultiProviderEvent: EventResponse = {
  id: 'event-drumsheds-amelie',
  name: 'Amelie Lens - Exhale London',
  venueName: 'Drumsheds',
  date: '2026-11-14',
  time: '14:00:00',
  ticketUrl: 'https://ra.co/events/1001',
  status: 'OnSale',
  provider: 'ResidentAdvisor',
  offers: [
    {
      provider: 'ResidentAdvisor',
      ticketUrl: 'https://ra.co/events/1001',
      status: 'OnSale',
    },
    {
      provider: 'Skiddle',
      ticketUrl: 'https://www.skiddle.com/e/1001',
      status: 'OnSale',
    },
    {
      provider: 'Ticketmaster',
      ticketUrl: 'https://www.ticketmaster.co.uk/event/1001',
      status: 'SoldOut',
    },
  ],
}

export const mockTwoProviderEvent: EventResponse = {
  id: 'event-fabric-bicep',
  name: 'Bicep (DJ Set)',
  venueName: 'Fabric',
  date: '2026-11-21',
  time: '23:00:00',
  ticketUrl: 'https://ra.co/events/1002',
  status: 'OnSale',
  provider: 'ResidentAdvisor',
  offers: [
    {
      provider: 'ResidentAdvisor',
      ticketUrl: 'https://ra.co/events/1002',
      status: 'OnSale',
    },
    {
      provider: 'Skiddle',
      ticketUrl: 'https://www.skiddle.com/e/1002',
      status: 'OnSale',
    },
  ],
}

export const mockSingleProviderEvent: EventResponse = {
  id: 'event-fold-charlotte',
  name: 'Charlotte de Witte - KNTXT London',
  venueName: 'FOLD',
  date: '2026-11-28',
  time: '22:00:00',
  ticketUrl: 'https://ra.co/events/1003',
  status: 'SoldOut',
  provider: 'ResidentAdvisor',
  offers: [
    {
      provider: 'ResidentAdvisor',
      ticketUrl: 'https://ra.co/events/1003',
      status: 'SoldOut',
    },
  ],
}

export const mockNoTicketUrlEvent: EventResponse = {
  id: 'event-mos-hardwell',
  name: 'Hardwell - London Special',
  venueName: 'Ministry of Sound',
  date: '2026-12-05',
  time: null,
  ticketUrl: null,
  status: 'OnSale',
  provider: 'Ticketmaster',
  offers: [
    {
      provider: 'Ticketmaster',
      ticketUrl: null,
      status: 'OnSale',
    },
  ],
}

export const mockDefaultEvents: EventResponse[] = [
  mockMultiProviderEvent,
  mockTwoProviderEvent,
  mockSingleProviderEvent,
  mockNoTicketUrlEvent,
]
