import type { EventStatus } from '../../api/types'

interface BadgeProps {
  status: EventStatus
  className?: string
}

const STATUS_CONFIG: Record<EventStatus, { label: string; styles: string }> = {
  OnSale: {
    label: 'On Sale',
    styles: 'bg-emerald-950/80 text-emerald-400 border-emerald-800/80',
  },
  SoldOut: {
    label: 'Sold Out',
    styles: 'bg-rose-950/80 text-rose-400 border-rose-800/80',
  },
  Postponed: {
    label: 'Postponed',
    styles: 'bg-amber-950/80 text-amber-400 border-amber-800/80',
  },
  Cancelled: {
    label: 'Cancelled',
    styles: 'bg-red-950/80 text-red-400 border-red-800/80',
  },
  Unknown: {
    label: 'Tickets',
    styles: 'bg-zinc-800 text-zinc-400 border-zinc-700',
  },
}

export function Badge({ status, className = '' }: BadgeProps) {
  const config = STATUS_CONFIG[status] ?? STATUS_CONFIG.Unknown

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${config.styles} ${className}`}
    >
      {config.label}
    </span>
  )
}
