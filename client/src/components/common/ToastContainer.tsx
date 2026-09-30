import { useToast } from '../../hooks/useToast'

export function ToastContainer() {
  const { toasts, dismissToast } = useToast()

  if (toasts.length === 0) {
    return null
  }

  return (
    <div
      aria-live="polite"
      aria-atomic="false"
      className="fixed bottom-5 right-5 z-50 flex flex-col gap-2 max-w-md w-full px-4 pointer-events-none"
    >
      {toasts.map((toast) => {
        const isSuccess = toast.type === 'success'
        const isError = toast.type === 'error'

        const bgStyles = isSuccess
          ? 'bg-emerald-950/95 border-emerald-500/40 text-emerald-100 shadow-emerald-950/40'
          : isError
            ? 'bg-rose-950/95 border-rose-500/40 text-rose-100 shadow-rose-950/40'
            : 'bg-zinc-900/95 border-zinc-700 text-zinc-100 shadow-zinc-950/40'

        const icon = isSuccess ? '✓' : isError ? '⚠' : 'ℹ'

        return (
          <div
            key={toast.id}
            role="status"
            className={`pointer-events-auto flex items-center justify-between gap-3 p-4 rounded-xl border shadow-lg backdrop-blur-md transition-all duration-200 ${bgStyles}`}
          >
            <div className="flex items-center gap-3">
              <span className="font-bold text-base flex-shrink-0" aria-hidden="true">
                {icon}
              </span>
              <p className="text-sm font-medium leading-snug">{toast.message}</p>
            </div>
            <button
              type="button"
              onClick={() => dismissToast(toast.id)}
              aria-label="Dismiss notification"
              className="min-h-[44px] min-w-[44px] -mr-2 -my-2 flex items-center justify-center text-sm opacity-70 hover:opacity-100 transition-opacity focus:outline-none focus-visible:ring-2 focus-visible:ring-white/40 rounded-lg cursor-pointer"
            >
              ✕
            </button>
          </div>
        )
      })}
    </div>
  )
}
