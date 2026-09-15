import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('Should_render_app_heading_and_tagline', () => {
    render(<App />)

    expect(screen.getByRole('heading', { level: 1, name: 'ElectronicLive' })).toBeInTheDocument()
    expect(screen.getByText('London EDM and Live Gig Tracker')).toBeInTheDocument()
  })
})
