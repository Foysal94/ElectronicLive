import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Badge } from '../Badge'

describe('Badge Component', () => {
  it('Should_render_on_sale_badge_with_correct_label_and_styles', () => {
    render(<Badge status="OnSale" />)

    const badge = screen.getByText('On Sale')
    expect(badge).toBeInTheDocument()
    expect(badge.className).toContain('text-emerald-400')
  })

  it('Should_render_sold_out_badge_with_correct_label_and_styles', () => {
    render(<Badge status="SoldOut" />)

    const badge = screen.getByText('Sold Out')
    expect(badge).toBeInTheDocument()
    expect(badge.className).toContain('text-rose-400')
  })

  it('Should_render_postponed_and_cancelled_badges_correctly', () => {
    const { rerender } = render(<Badge status="Postponed" />)
    expect(screen.getByText('Postponed')).toBeInTheDocument()

    rerender(<Badge status="Cancelled" />)
    expect(screen.getByText('Cancelled')).toBeInTheDocument()
  })
})
