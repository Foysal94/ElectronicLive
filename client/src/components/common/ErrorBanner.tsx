export interface ErrorBannerProps {
  title?: string
  message?: string
  onRetry?: () => void
  className?: string
  id?: string
}

export function ErrorBanner({
  title = 'Service Connection Error',
  message = 'Unable to reach the London events service. Please ensure the backend API is running or try again.',
  onRetry,
  className = '',
  id,
}: ErrorBannerProps) {
  return (
    <aside
      id={id}
      role="alert"
      className={`rounded-xl border border-rose-900/50 bg-[#22262d] p-5 text-center shadow-lg ${className}`}
    >
      <div className="flex flex-col items-center justify-center gap-3">
        <div className="flex items-center gap-2 text-rose-400">
          <svg
            className="w-5 h-5 flex-shrink-0"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            />
          </svg>
          <h3 className="text-base font-semibold text-white">{title}</h3>
        </div>
        <p className="text-sm text-gray-400 max-w-md">{message}</p>
        {onRetry && (
          <button
            type="button"
            onClick={onRetry}
            className="mt-1 inline-flex items-center justify-center min-h-[44px] px-5 py-2 rounded-lg bg-rose-950 text-rose-300 hover:bg-rose-900 hover:text-white border border-rose-800 transition-colors font-medium text-sm focus:outline-none focus:ring-2 focus:ring-rose-500"
          >
            Try Again
          </button>
        )}
      </div>
    </aside>
  )
}
