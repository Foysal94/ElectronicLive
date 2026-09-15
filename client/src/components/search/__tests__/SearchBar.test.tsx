import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { SearchBar } from '../SearchBar'

describe('SearchBar Component', () => {
  it('Should_trigger_search_on_form_submit', async () => {
    const user = userEvent.setup()
    const handleSearch = vi.fn()
    const handleChange = vi.fn()

    render(
      <SearchBar
        value="Amelie Lens"
        onChange={handleChange}
        onSearch={handleSearch}
      />
    )

    const submitButton = screen.getByRole('button', { name: 'Search' })
    await user.click(submitButton)

    expect(handleSearch).toHaveBeenCalledTimes(1)
    expect(handleSearch).toHaveBeenCalledWith('Amelie Lens')
  })

  it('Should_not_trigger_search_on_keystroke', async () => {
    const user = userEvent.setup()
    const handleSearch = vi.fn()
    const handleChange = vi.fn()

    render(
      <SearchBar
        value=""
        onChange={handleChange}
        onSearch={handleSearch}
      />
    )

    const input = screen.getByRole('searchbox', {
      name: 'Search artist, event, or venue in London',
    })
    await user.type(input, 'Bicep')

    expect(handleChange).toHaveBeenCalled()
    expect(handleSearch).not.toHaveBeenCalled()
  })

  it('Should_clear_search_input_when_clear_button_clicked', async () => {
    const user = userEvent.setup()
    const handleChange = vi.fn()
    const handleClear = vi.fn()
    const handleSearch = vi.fn()

    render(
      <SearchBar
        value="Drumsheds"
        onChange={handleChange}
        onSearch={handleSearch}
        onClear={handleClear}
      />
    )

    const clearButton = screen.getByRole('button', { name: 'Clear search' })
    await user.click(clearButton)

    expect(handleChange).toHaveBeenCalledWith('')
    expect(handleClear).toHaveBeenCalledTimes(1)
  })
})
