import type { FormEvent, RefObject } from 'react'

interface SubscriptionFormProps {
  email: string
  onChangeEmail: (value: string) => void
  onSubmit: (e: FormEvent<HTMLFormElement>) => void
  onCancel: () => void
  isPending: boolean
  hasError: boolean
  inputRef?: RefObject<HTMLInputElement | null>
}

export function SubscriptionForm({
  email,
  onChangeEmail,
  onSubmit,
  onCancel,
  isPending,
  hasError,
  inputRef,
}: SubscriptionFormProps) {
  return (
    <form onSubmit={onSubmit} className="space-y-5" noValidate>
      <div className="space-y-2">
        <label htmlFor="track-email-input" className="block text-sm font-medium text-gray-300">
          Email Address
        </label>
        <input
          ref={inputRef}
          id="track-email-input"
          type="email"
          value={email}
          onChange={(e) => onChangeEmail(e.target.value)}
          placeholder="you@example.com"
          disabled={isPending}
          aria-invalid={hasError}
          aria-describedby={hasError ? 'track-error-message' : undefined}
          className="w-full min-h-[44px] px-4 py-2.5 rounded-xl bg-[#181b1f] border border-white/10 text-white placeholder-gray-500 focus:outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-50 disabled:cursor-not-allowed text-sm transition-all"
        />
      </div>

      <div className="flex items-center justify-end gap-3 pt-2">
        <button
          type="button"
          onClick={onCancel}
          disabled={isPending}
          className="min-h-[44px] px-5 py-2.5 rounded-xl text-sm font-medium text-gray-300 hover:text-white hover:bg-white/5 transition-colors disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-white/30"
        >
          Cancel
        </button>

        <button
          type="submit"
          disabled={isPending}
          className="min-h-[44px] px-6 py-2.5 rounded-xl text-sm font-semibold bg-emerald-500 hover:bg-emerald-400 active:bg-emerald-600 text-gray-950 flex items-center justify-center gap-2 transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-400"
        >
          {isPending ? (
            <>
              <svg
                className="animate-spin h-4 w-4 text-gray-950"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
              <span>Subscribing...</span>
            </>
          ) : (
            <span>Subscribe</span>
          )}
        </button>
      </div>
    </form>
  )
}
