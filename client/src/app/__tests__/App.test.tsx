import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { createQueryWrapper } from '../../test/utils'
import App from '../App'

describe('App Integration Suite', () => {
  beforeEach(() => {
    window.history.replaceState(null, '', '/')
  })

  afterEach(() => {
    window.history.replaceState(null, '', '/')
  })

  it('Should_display_idle_prompt_on_initial_load', () => {
    render(<App />, { wrapper: createQueryWrapper() })

    expect(screen.getByRole('heading', { level: 1, name: 'ElectronicLive' })).toBeInTheDocument()
    expect(screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })).toHaveValue('')
    expect(screen.getByText('Quick Search Artists:')).toBeInTheDocument()
    expect(screen.getByText('Quick Search Venues:')).toBeInTheDocument()
    expect(screen.getByText('Discover London Electronic Music')).toBeInTheDocument()
  })

  it('Should_initialize_search_from_url_query_parameter', async () => {
    window.history.replaceState(null, '', '/?q=Amelie')

    render(<App />, { wrapper: createQueryWrapper() })

    const input = screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })
    expect(input).toHaveValue('Amelie')

    await waitFor(() => {
      expect(screen.getAllByText('Amelie Lens - Exhale London').length).toBeGreaterThan(0)
    })
  })

  it('Should_execute_search_and_render_events_when_pill_is_clicked', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const fabricPill = screen.getByRole('button', { name: 'Fabric' })
    await user.click(fabricPill)

    expect(screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })).toHaveValue('Fabric')
    expect(window.location.search).toBe('?q=Fabric')

    await waitFor(() => {
      expect(screen.getAllByText('Bicep (DJ Set)').length).toBeGreaterThan(0)
    })
  })

  it('Should_execute_search_when_search_form_is_submitted', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const input = screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })
    await user.type(input, 'Bicep')

    const searchButton = screen.getByRole('button', { name: 'Search' })
    await user.click(searchButton)

    expect(window.location.search).toBe('?q=Bicep')

    await waitFor(() => {
      expect(screen.getAllByText('Bicep (DJ Set)').length).toBeGreaterThan(0)
    })
  })

  it('Should_display_empty_state_when_no_events_found', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const input = screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })
    await user.type(input, 'NonexistentArtist')

    const searchButton = screen.getByRole('button', { name: 'Search' })
    await user.click(searchButton)

    await waitFor(() => {
      expect(screen.getByText('No Gigs Found')).toBeInTheDocument()
    })
    expect(screen.getByText(/No upcoming London gigs found for/i)).toBeInTheDocument()
    expect(screen.getByText('"NonexistentArtist"')).toBeInTheDocument()
  })

  it('Should_display_error_banner_and_retry_when_backend_fails', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const input = screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })
    await user.type(input, 'error-500')

    const searchButton = screen.getByRole('button', { name: 'Search' })
    await user.click(searchButton)

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })
    expect(screen.getByText('Internal failure occurred while querying events.')).toBeInTheDocument()

    const retryButton = screen.getByRole('button', { name: 'Try Again' })
    expect(retryButton).toBeInTheDocument()
    await user.click(retryButton)
  })

  it('Should_clear_search_and_return_to_idle_state_when_clear_clicked', async () => {
    const user = userEvent.setup()
    window.history.replaceState(null, '', '/?q=Amelie')

    render(<App />, { wrapper: createQueryWrapper() })

    await waitFor(() => {
      expect(screen.getAllByText('Amelie Lens - Exhale London').length).toBeGreaterThan(0)
    })

    const clearButton = screen.getByRole('button', { name: 'Clear search' })
    await user.click(clearButton)

    expect(screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })).toHaveValue('')
    expect(window.location.search).toBe('')
    expect(screen.getByText('Discover London Electronic Music')).toBeInTheDocument()
  })

  it('Should_update_search_state_on_browser_popstate', async () => {
    window.history.replaceState(null, '', '/?q=Amelie')

    render(<App />, { wrapper: createQueryWrapper() })

    await waitFor(() => {
      expect(screen.getAllByText('Amelie Lens - Exhale London').length).toBeGreaterThan(0)
    })

    // Simulate browser back button navigation to root
    act(() => {
      window.history.replaceState(null, '', '/')
      window.dispatchEvent(new PopStateEvent('popstate'))
    })

    await waitFor(() => {
      expect(screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })).toHaveValue('')
      expect(screen.getByText('Discover London Electronic Music')).toBeInTheDocument()
    })
  })

  it('Should_execute_search_and_render_events_when_genre_pill_clicked', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const technoPill = screen.getByRole('button', { name: 'Techno' })
    await user.click(technoPill)

    expect(screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })).toHaveValue('')
    expect(window.location.search).toBe('?genre=techno')

    await waitFor(() => {
      expect(screen.getAllByText('Anetha & Charlotte de Witte - Techno All Night').length).toBeGreaterThan(0)
    })
  })

  it('Should_initialize_search_from_url_genre_parameter', async () => {
    window.history.replaceState(null, '', '/?genre=house')

    render(<App />, { wrapper: createQueryWrapper() })

    await waitFor(() => {
      expect(screen.getAllByText('Defected London - House Odyssey').length).toBeGreaterThan(0)
    })

    const housePill = screen.getByRole('button', { name: 'House' })
    expect(housePill.className).toContain('text-emerald-400')
    expect(housePill.className).toContain('bg-emerald-950')
  })

  it('Should_switch_cleanly_between_genre_and_text_search', async () => {
    const user = userEvent.setup()
    window.history.replaceState(null, '', '/?genre=techno')

    render(<App />, { wrapper: createQueryWrapper() })

    await waitFor(() => {
      expect(screen.getAllByText('Anetha & Charlotte de Witte - Techno All Night').length).toBeGreaterThan(0)
    })

    // Switch to text search
    const input = screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })
    await user.type(input, 'Bicep')
    const searchButton = screen.getByRole('button', { name: 'Search' })
    await user.click(searchButton)

    expect(window.location.search).toBe('?q=Bicep')
    await waitFor(() => {
      expect(screen.getAllByText('Bicep (DJ Set)').length).toBeGreaterThan(0)
    })

    // Switch back to genre pill
    const dnbPill = screen.getByRole('button', { name: 'Drum & Bass' })
    await user.click(dnbPill)

    expect(window.location.search).toBe('?genre=drum-and-bass')
    expect(screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })).toHaveValue('')
    await waitFor(() => {
      expect(screen.getAllByText('Hospitality London - Drum & Bass Special').length).toBeGreaterThan(0)
    })
  })

  it('Should_open_modal_and_subscribe_successfully_from_search_results', async () => {
    const user = userEvent.setup()
    window.history.replaceState(null, '', '/?q=Bicep')

    render(<App />, { wrapper: createQueryWrapper() })

    await waitFor(() => {
      expect(screen.getAllByText('Bicep (DJ Set)').length).toBeGreaterThan(0)
    })

    const trackButton = screen.getByRole('button', { name: 'Track Bicep' })
    await user.click(trackButton)

    const dialog = screen.getByRole('dialog')
    expect(dialog).toBeInTheDocument()

    const emailInput = screen.getByLabelText(/email address/i)
    await user.type(emailInput, 'fan@example.com')

    const subscribeBtn = screen.getByRole('button', { name: /^subscribe$/i })
    await user.click(subscribeBtn)

    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 3, name: "You're tracking Bicep!" })).toBeInTheDocument()
    })

    expect(screen.getByText(/fan@example\.com/)).toBeInTheDocument()

    const doneBtn = screen.getByRole('button', { name: /^done$/i })
    await user.click(doneBtn)

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  it('Should_open_tracking_modal_from_empty_state_action', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const input = screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })
    await user.type(input, 'Floating Points')

    const searchButton = screen.getByRole('button', { name: 'Search' })
    await user.click(searchButton)

    await waitFor(() => {
      expect(screen.getByText('No Gigs Found')).toBeInTheDocument()
    })

    const trackButton = screen.getByRole('button', { name: 'Track Floating Points' })
    await user.click(trackButton)

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /track floating points/i })).toBeInTheDocument()
  })

  it('Should_initialize_search_and_render_events_from_url_date_parameters', async () => {
    window.history.replaceState(null, '', '/?from=2026-11-14&to=2026-11-18')

    render(<App />, { wrapper: createQueryWrapper() })

    await waitFor(() => {
      expect(screen.getAllByText('Amelie Lens - Exhale London').length).toBeGreaterThan(0)
    })
  })

  it('Should_preserve_date_filter_when_clearing_search_text', async () => {
    const user = userEvent.setup()
    window.history.replaceState(null, '', '/?q=Amelie&from=2026-11-14&to=2026-11-18')

    render(<App />, { wrapper: createQueryWrapper() })

    await waitFor(() => {
      expect(screen.getAllByText('Amelie Lens - Exhale London').length).toBeGreaterThan(0)
    })

    const clearButton = screen.getByRole('button', { name: 'Clear search' })
    await user.click(clearButton)

    expect(screen.getByRole('searchbox', { name: 'Search artist, event, or venue in London' })).toHaveValue('')
    expect(window.location.search).toBe('?from=2026-11-14&to=2026-11-18')
  })

  it('Should_filter_events_by_date_when_preset_pill_clicked', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const tonightBtn = screen.getByRole('button', { name: 'Tonight' })
    await user.click(tonightBtn)

    expect(tonightBtn).toHaveClass('bg-emerald-950')
    expect(window.location.search).toMatch(/^\?from=\d{4}-\d{2}-\d{2}&to=\d{4}-\d{2}-\d{2}$/)
  })

  it('Should_toggle_date_preset_off_when_active_preset_clicked_again', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    const tonightBtn = screen.getByRole('button', { name: 'Tonight' })
    await user.click(tonightBtn)
    expect(tonightBtn).toHaveClass('bg-emerald-950')

    await user.click(tonightBtn)
    expect(tonightBtn).not.toHaveClass('bg-emerald-950')
    expect(window.location.search).toBe('')
  })

  it('Should_filter_events_when_custom_date_range_is_applied_via_tray', async () => {
    const user = userEvent.setup()
    render(<App />, { wrapper: createQueryWrapper() })

    await user.click(screen.getByRole('button', { name: 'Custom...' }))
    await user.click(screen.getByRole('button', { name: /next month/i }))

    const nov14 = screen.getByRole('button', { name: /November 14th,/i })
    const nov18 = screen.getByRole('button', { name: /November 18th,/i })
    await user.click(nov14)
    await user.click(nov18)

    const applyButton = screen.getByRole('button', { name: 'Apply' })
    await user.click(applyButton)

    expect(window.location.search).toBe('?from=2026-11-14&to=2026-11-18')
    await waitFor(() => {
      expect(screen.getAllByText('Amelie Lens - Exhale London').length).toBeGreaterThan(0)
    })
  })
})

