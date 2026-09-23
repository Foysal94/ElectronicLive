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
    { provider: 'ResidentAdvisor', ticketUrl: 'https://ra.co/events/1001', status: 'OnSale' },
    { provider: 'Skiddle', ticketUrl: 'https://www.skiddle.com/e/1001', status: 'OnSale' },
    { provider: 'Ticketmaster', ticketUrl: 'https://ticketmaster.co.uk/event/1001', status: 'SoldOut' },
  ],
}

export const mockTwoProviderEvent: EventResponse = {
  id: 'event-fold-charlotte',
  name: 'Charlotte de Witte - KNTXT London',
  venueName: 'FOLD',
  date: '2026-12-05',
  time: '23:00:00',
  ticketUrl: 'https://ra.co/events/2001',
  status: 'OnSale',
  provider: 'ResidentAdvisor',
  offers: [
    { provider: 'ResidentAdvisor', ticketUrl: 'https://ra.co/events/2001', status: 'OnSale' },
    { provider: 'Skiddle', ticketUrl: 'https://www.skiddle.com/e/2001', status: 'OnSale' },
  ],
}

export const mockSingleProviderEvent: EventResponse = {
  id: 'event-fabric-bicep',
  name: 'Bicep (DJ Set)',
  venueName: 'Fabric',
  date: '2026-11-21',
  time: '22:00:00',
  ticketUrl: null,
  status: 'OnSale',
  provider: 'ResidentAdvisor',
  offers: [
    { provider: 'ResidentAdvisor', ticketUrl: null, status: 'OnSale' },
  ],
}

export const mockTbaDateEvent: EventResponse = {
  id: 'event-mos-hardwell',
  name: 'Hardwell - London Special',
  venueName: 'Ministry of Sound',
  date: null,
  time: null,
  ticketUrl: 'https://ticketmaster.co.uk/event/3001',
  status: 'OnSale',
  provider: 'Ticketmaster',
  offers: [
    { provider: 'Ticketmaster', ticketUrl: 'https://ticketmaster.co.uk/event/3001', status: 'OnSale' },
  ],
}

export const mockTechnoEvent: EventResponse = {
  id: 'event-fold-techno',
  name: 'Anetha & Charlotte de Witte - Techno All Night',
  venueName: 'FOLD',
  date: '2026-12-05',
  time: '23:00:00',
  ticketUrl: 'https://ra.co/events/2001',
  status: 'OnSale',
  provider: 'ResidentAdvisor',
  offers: [
    { provider: 'ResidentAdvisor', ticketUrl: 'https://ra.co/events/2001', status: 'OnSale' },
  ],
}

export const mockHouseEvent: EventResponse = {
  id: 'event-studio338-house',
  name: 'Defected London - House Odyssey',
  venueName: 'Studio 338',
  date: '2026-11-28',
  time: '21:00:00',
  ticketUrl: 'https://www.skiddle.com/e/3001',
  status: 'OnSale',
  provider: 'Skiddle',
  offers: [
    { provider: 'Skiddle', ticketUrl: 'https://www.skiddle.com/e/3001', status: 'OnSale' },
  ],
}

export const mockDnbEvent: EventResponse = {
  id: 'event-drumsheds-dnb',
  name: 'Hospitality London - Drum & Bass Special',
  venueName: 'Drumsheds',
  date: '2026-12-12',
  time: '14:00:00',
  ticketUrl: 'https://ticketmaster.co.uk/event/4001',
  status: 'OnSale',
  provider: 'Ticketmaster',
  offers: [
    { provider: 'Ticketmaster', ticketUrl: 'https://ticketmaster.co.uk/event/4001', status: 'OnSale' },
  ],
}

export const mockDefaultEvents: EventResponse[] = [
  mockMultiProviderEvent,
  mockTwoProviderEvent,
  mockSingleProviderEvent,
  mockTbaDateEvent,
  mockTechnoEvent,
  mockHouseEvent,
  mockDnbEvent,
]
