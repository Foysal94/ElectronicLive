import { useState } from 'react'
import { type DateRange as DayPickerRange, DayPicker } from 'react-day-picker'
import {
  DATE_PRESET_KEYS,
  DATE_PRESETS,
  type DatePresetKey,
  formatDateOnly,
  getDaysDifference,
  matchActivePreset,
  parseDateOnly,
} from './datePresets'

export interface DateFilterBarProps {
  activeFrom?: string
  activeTo?: string
  onSelectDateRange: (from?: string, to?: string) => void
  hasSearchContext?: boolean
  className?: string
}

interface DayPickerCssVariables extends React.CSSProperties {
  '--rdp-accent-color'?: string
  '--rdp-accent-background-color'?: string
  '--rdp-range_middle-background-color'?: string
  '--rdp-range_middle-color'?: string
  '--rdp-day-height'?: string
  '--rdp-day-width'?: string
  '--rdp-day_button-height'?: string
  '--rdp-day_button-width'?: string
}

const dayPickerDarkStyles: DayPickerCssVariables = {
  '--rdp-accent-color': '#10b981',
  '--rdp-accent-background-color': '#064e3b',
  '--rdp-range_middle-background-color': '#064e3b',
  '--rdp-range_middle-color': '#34d399',
  '--rdp-day-height': '44px',
  '--rdp-day-width': '44px',
  '--rdp-day_button-height': '44px',
  '--rdp-day_button-width': '44px',
}

function resolveRangeFromStrings(fromStr?: string, toStr?: string): DayPickerRange | undefined {
  if (!fromStr || !toStr) return undefined
  const from = parseDateOnly(fromStr)
  const to = parseDateOnly(toStr)
  return from && to ? { from, to } : undefined
}

export function DateFilterBar({
  activeFrom = '',
  activeTo = '',
  onSelectDateRange,
  hasSearchContext = false,
  className = '',
}: DateFilterBarProps) {
  const [isCustomTrayOpen, setIsCustomTrayOpen] = useState(false)
  const [selectedRange, setSelectedRange] = useState<DayPickerRange | undefined>(() =>
    resolveRangeFromStrings(activeFrom, activeTo)
  )

  const activePreset = matchActivePreset(activeFrom, activeTo)
  const isCustomActive = Boolean(activeFrom && activeTo && !activePreset)

  const handlePresetClick = (presetId: DatePresetKey) => {
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
    if (!isCustomTrayOpen) {
      setSelectedRange(resolveRangeFromStrings(activeFrom, activeTo))
    }
    setIsCustomTrayOpen((prev) => !prev)
  }

  const handleCancelCustom = () => {
    setIsCustomTrayOpen(false)
    setSelectedRange(resolveRangeFromStrings(activeFrom, activeTo))
  }

  const handleClearCustom = () => {
    onSelectDateRange('', '')
    setIsCustomTrayOpen(false)
    setSelectedRange(undefined)
  }

  const fromIso = selectedRange?.from ? formatDateOnly(selectedRange.from) : ''
  const toIso = selectedRange?.to ? formatDateOnly(selectedRange.to) : ''
  const hasBothDates = Boolean(fromIso && toIso)
  const rangeDays = hasBothDates ? getDaysDifference(fromIso, toIso) : 0
  const exceedsDateOnlyLimit = !hasSearchContext && rangeDays > 7
  const isApplyEnabled = hasBothDates && !exceedsDateOnlyLimit

  const handleApplyCustom = (e: React.FormEvent) => {
    e.preventDefault()
    if (!isApplyEnabled || !fromIso || !toIso) return

    onSelectDateRange(fromIso, toIso)
    setIsCustomTrayOpen(false)
  }

  return (
    <section
      aria-label="Filter events by date"
      className={`w-full max-w-4xl mx-auto flex flex-col gap-3 text-left ${className}`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3">
        <span className="text-xs font-semibold uppercase tracking-wider text-gray-400 sm:w-48 flex-shrink-0">
          Filter by Date:
        </span>
        <div className="flex flex-wrap gap-2">
          {DATE_PRESETS.map((preset) => {
            const isActive = activePreset === preset.id
            const isNext30 = preset.id === DATE_PRESET_KEYS.NEXT_30_DAYS
            const isDisabled = isNext30 && !hasSearchContext

            return (
              <button
                key={preset.id}
                type="button"
                aria-pressed={isActive}
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
            aria-controls="custom-date-tray"
            className={`min-h-[44px] px-4 py-2 rounded-full text-xs sm:text-sm font-medium border transition-colors flex items-center justify-center ${
              isCustomActive
                ? 'bg-emerald-950 text-emerald-400 border-emerald-800 shadow-sm'
                : isCustomTrayOpen
                  ? 'bg-[#2b3039] text-white border-white/30'
                  : 'bg-[#22262d] text-gray-300 border-white/10 hover:bg-[#2b3039] hover:text-white hover:border-white/20'
            }`}
          >
            Custom...
          </button>
        </div>
      </div>

      {isCustomTrayOpen && (
        <div className="flex justify-start">
          <form
            id="custom-date-tray"
            onSubmit={handleApplyCustom}
            aria-label="Custom date range selector"
            className="w-full sm:w-auto bg-[#22262d] border border-white/10 rounded-2xl p-4 shadow-2xl flex flex-col gap-3"
          >
            <div className="text-xs font-medium text-emerald-400 bg-emerald-950/60 border border-emerald-800/60 px-3 py-1.5 rounded-lg text-center">
              {selectedRange?.from && selectedRange?.to
                ? `${formatDateOnly(selectedRange.from)} → ${formatDateOnly(selectedRange.to)} (${rangeDays} days)`
                : selectedRange?.from
                  ? `From: ${formatDateOnly(selectedRange.from)} (select end date)`
                  : 'Select start and end dates'}
            </div>

            <DayPicker
              mode="range"
              selected={selectedRange}
              onSelect={setSelectedRange}
              defaultMonth={selectedRange?.from ?? new Date()}
              style={dayPickerDarkStyles}
              classNames={{
                root: 'p-1 bg-[#181b1f] text-gray-200 rounded-xl border border-white/10 inline-block',
                month_caption: 'flex justify-center items-center h-10 relative text-sm font-semibold text-white',
                caption_label: 'text-sm font-semibold text-white',
                nav: 'flex items-center justify-between w-full absolute inset-x-0 px-1',
                button_previous: 'min-h-[44px] min-w-[44px] text-gray-400 hover:text-white rounded-md hover:bg-white/10 flex items-center justify-center transition-colors',
                button_next: 'min-h-[44px] min-w-[44px] text-gray-400 hover:text-white rounded-md hover:bg-white/10 flex items-center justify-center transition-colors',
                month_grid: 'w-full border-collapse mt-2',
                weekdays: 'flex text-xs text-gray-400 font-medium pb-1',
                weekday: 'w-9 text-center',
                weeks: 'flex flex-col gap-1',
                week: 'flex w-full',
                day: 'p-0 text-center text-sm relative flex items-center justify-center',
                day_button: 'min-h-[44px] min-w-[44px] w-9 h-9 rounded-lg text-gray-200 hover:bg-white/10 hover:text-white flex items-center justify-center transition-colors',
                selected: 'bg-emerald-950 text-emerald-400 font-semibold',
                range_start: 'bg-emerald-600 text-white font-bold rounded-l-lg',
                range_end: 'bg-emerald-600 text-white font-bold rounded-r-lg',
                range_middle: 'bg-emerald-950 text-emerald-300 rounded-none',
                today: 'text-emerald-400 font-bold',
                outside: 'text-gray-600 opacity-40',
                disabled: 'text-gray-600 opacity-30 cursor-not-allowed',
              }}
            />

            <div className="flex items-center gap-2 pt-1">
              <button
                type="submit"
                disabled={!isApplyEnabled}
                className="flex-1 min-h-[44px] px-4 py-2 rounded-lg bg-emerald-500 hover:bg-emerald-400 disabled:opacity-40 text-black font-semibold text-sm transition-colors cursor-pointer disabled:cursor-not-allowed flex items-center justify-center"
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
              {selectedRange?.from && (
                <button
                  type="button"
                  onClick={handleClearCustom}
                  className="min-h-[44px] px-3 py-2 text-xs text-gray-400 hover:text-white transition-colors flex items-center justify-center"
                >
                  Clear
                </button>
              )}
            </div>

            {exceedsDateOnlyLimit && (
              <p role="alert" className="text-xs text-amber-400 flex items-center gap-1.5 pt-1">
                <span>⚠️</span>
                <span>
                  Date-only search exceeds 7-day limit. Add an artist or genre for longer ranges.
                </span>
              </p>
            )}
          </form>
        </div>
      )}
    </section>
  )
}
