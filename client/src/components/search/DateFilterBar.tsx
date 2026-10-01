import { useState } from 'react'
import {
  DATE_PRESETS,
  getDaysDifference,
  matchActivePreset,
} from './datePresets'

export interface DateFilterBarProps {
  activeFrom?: string
  activeTo?: string
  onSelectDateRange: (from?: string, to?: string) => void
  hasSearchContext?: boolean
  className?: string
}

export function DateFilterBar({
  activeFrom = '',
  activeTo = '',
  onSelectDateRange,
  hasSearchContext = false,
  className = '',
}: DateFilterBarProps) {
  const [isCustomTrayOpen, setIsCustomTrayOpen] = useState(false)
  const [customFrom, setCustomFrom] = useState(activeFrom)
  const [customTo, setCustomTo] = useState(activeTo)

  const activePreset = matchActivePreset(activeFrom, activeTo)
  const isCustomActive = Boolean(activeFrom && activeTo && !activePreset)

  const handlePresetClick = (presetId: string) => {
    if (activePreset === presetId) {
      onSelectDateRange('', '')
      return
    }

    const targetPreset = DATE_PRESETS.find((p) => p.id === presetId)
    if (!targetPreset) return

    const { from, to } = targetPreset.getRange()
    onSelectDateRange(from, to)
    setIsCustomTrayOpen(false)
  }

  const handleCustomToggle = () => {
    setIsCustomTrayOpen((prev) => {
      const next = !prev
      if (next) {
        setCustomFrom(activeFrom)
        setCustomTo(activeTo)
      }
      return next
    })
  }

  const handleCancelCustom = () => {
    setIsCustomTrayOpen(false)
    setCustomFrom(activeFrom)
    setCustomTo(activeTo)
  }

  const hasBothDates = Boolean(customFrom && customTo)
  const isOrderValid = hasBothDates && customTo >= customFrom
  const rangeDays = hasBothDates ? getDaysDifference(customFrom, customTo) : 0
  const exceedsDateOnlyLimit = !hasSearchContext && rangeDays > 7
  const isApplyEnabled = isOrderValid && !exceedsDateOnlyLimit

  const handleApplyCustom = (e: React.FormEvent) => {
    e.preventDefault()
    if (!isApplyEnabled) return

    onSelectDateRange(customFrom, customTo)
    setIsCustomTrayOpen(false)
  }

  return (
    <section
      aria-label="Filter events by date"
      className={`w-full max-w-4xl mx-auto flex flex-col gap-3 text-left ${className}`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3">
        <span className="text-xs font-semibold uppercase tracking-wider text-gray-400 flex-shrink-0">
          Filter by Date:
        </span>
        <div className="flex flex-wrap gap-2">
          {DATE_PRESETS.map((preset) => {
            const isActive = activePreset === preset.id
            const isNext30 = preset.id === 'next-30-days'
            const isDisabled = isNext30 && !hasSearchContext

            return (
              <button
                key={preset.id}
                type="button"
                disabled={isDisabled}
                onClick={() => handlePresetClick(preset.id)}
                title={
                  isDisabled
                    ? 'Artist or genre required for date ranges exceeding 7 days'
                    : undefined
                }
                className={`min-h-[44px] px-4 py-2 rounded-full text-xs sm:text-sm font-medium border transition-colors flex items-center justify-center ${
                  isDisabled
                    ? 'opacity-50 cursor-not-allowed bg-[#1e2229] text-gray-500 border-white/5'
                    : isActive
                      ? 'bg-emerald-950 text-emerald-400 border-emerald-800 shadow-sm'
                      : 'bg-[#22262d] text-gray-300 border-white/10 hover:bg-[#2b3039] hover:text-white hover:border-white/20'
                }`}
              >
                {preset.label}
              </button>
            )
          })}

          <button
            type="button"
            onClick={handleCustomToggle}
            aria-expanded={isCustomTrayOpen}
            className={`min-h-[44px] px-4 py-2 rounded-full text-xs sm:text-sm font-medium border transition-colors flex items-center justify-center ${
              isCustomActive || isCustomTrayOpen
                ? 'bg-emerald-950 text-emerald-400 border-emerald-800 shadow-sm'
                : 'bg-[#22262d] text-gray-300 border-white/10 hover:bg-[#2b3039] hover:text-white hover:border-white/20'
            }`}
          >
            Custom...
          </button>
        </div>
      </div>

      {isCustomTrayOpen && (
        <form
          onSubmit={handleApplyCustom}
          aria-label="Custom date range selector"
          className="bg-[#22262d]/70 border border-white/10 rounded-xl p-4 sm:p-5 flex flex-col gap-3 shadow-lg backdrop-blur-sm transition-all"
        >
          <div className="flex flex-col sm:flex-row sm:items-end gap-3 sm:gap-4">
            <div className="flex flex-col gap-1.5 flex-1">
              <label
                htmlFor="custom-from-date"
                className="text-xs font-medium text-gray-300"
              >
                From
              </label>
              <input
                id="custom-from-date"
                type="date"
                aria-label="From date"
                value={customFrom}
                onChange={(e) => setCustomFrom(e.target.value)}
                className="min-h-[44px] px-3 py-2 bg-[#181b1f] border border-white/15 rounded-lg text-sm text-gray-200 focus:outline-none focus:border-emerald-500 [color-scheme:dark] transition-colors"
              />
            </div>

            <div className="flex flex-col gap-1.5 flex-1">
              <label
                htmlFor="custom-to-date"
                className="text-xs font-medium text-gray-300"
              >
                To
              </label>
              <input
                id="custom-to-date"
                type="date"
                aria-label="To date"
                value={customTo}
                onChange={(e) => setCustomTo(e.target.value)}
                className="min-h-[44px] px-3 py-2 bg-[#181b1f] border border-white/15 rounded-lg text-sm text-gray-200 focus:outline-none focus:border-emerald-500 [color-scheme:dark] transition-colors"
              />
            </div>

            <div className="flex items-center gap-2 pt-1 sm:pt-0">
              <button
                type="submit"
                disabled={!isApplyEnabled}
                className="min-h-[44px] px-5 py-2 rounded-lg bg-emerald-500 hover:bg-emerald-400 disabled:opacity-50 disabled:hover:bg-emerald-500 text-black font-semibold text-sm transition-colors cursor-pointer disabled:cursor-not-allowed flex items-center justify-center"
              >
                Apply
              </button>
              <button
                type="button"
                onClick={handleCancelCustom}
                className="min-h-[44px] px-4 py-2 rounded-lg bg-[#2a2f37] hover:bg-[#343b45] text-gray-300 hover:text-white text-sm transition-colors flex items-center justify-center"
              >
                Cancel
              </button>
            </div>
          </div>

          {exceedsDateOnlyLimit && (
            <p className="text-xs text-amber-400 flex items-center gap-1.5 pt-1">
              <span>⚠️</span>
              <span>
                Date-only search exceeds 7-day limit. Add an artist or genre for longer ranges.
              </span>
            </p>
          )}

          {hasBothDates && customTo < customFrom && (
            <p className="text-xs text-rose-400 flex items-center gap-1.5 pt-1">
              <span>⚠️</span>
              <span>&apos;To&apos; date must be on or after &apos;From&apos; date.</span>
            </p>
          )}
        </form>
      )}
    </section>
  )
}
