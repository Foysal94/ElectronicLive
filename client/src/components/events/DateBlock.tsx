import { formatTime, parseDateOnly } from './utils'

interface DateBlockProps {
  date: string | null
  time?: string | null
  className?: string
}

export function DateBlock({ date, time, className = '' }: DateBlockProps) {
  const parsed = parseDateOnly(date)
  const formattedTime = formatTime(time)

  return (
    <div
      className={`bg-[#181b1f] border border-white/10 rounded-xl p-2 sm:p-2.5 flex flex-col items-center justify-center min-w-[72px] sm:min-w-[80px] text-center flex-shrink-0 shadow-inner ${className}`}
    >
      {parsed ? (
        <>
          <span className="text-[10px] sm:text-xs font-semibold text-emerald-400 tracking-wider">
            {parsed.dayOfWeek}
          </span>
          <span className="text-xl sm:text-2xl font-black text-white leading-none my-1">
            {parsed.day}
          </span>
          <span className="text-[10px] sm:text-xs font-medium text-gray-400">
            {parsed.month}
          </span>
          {formattedTime && (
            <span className="mt-1 text-[10px] text-gray-400 font-mono">
              {formattedTime}
            </span>
          )}
        </>
      ) : (
        <>
          <span className="text-[10px] sm:text-xs font-semibold text-amber-400 tracking-wider">
            DATE
          </span>
          <span className="text-lg sm:text-xl font-black text-white leading-tight my-1">
            TBA
          </span>
          <span className="text-[10px] text-gray-400">TBD</span>
        </>
      )}
    </div>
  )
}
