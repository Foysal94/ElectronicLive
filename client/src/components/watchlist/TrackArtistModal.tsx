import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import type { SubscriptionResponse } from '../../api/types'
import { useCreateSubscription } from '../../hooks/useCreateSubscription'
import { ErrorBanner } from '../common/ErrorBanner'


interface TrackArtistModalProps {
  isOpen: boolean
  artistName: string
  onClose: () => void
  onSubscribed?: (response: SubscriptionResponse) => void
}

// RFC 5322 standard regex for client-side structural validation
const RFC_EMAIL_REGEX =
  /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$/

export function TrackArtistModal({
  isOpen,
  artistName,
  onClose,
  onSubscribed,
}: TrackArtistModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const [email, setEmail] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)

  const mutation = useCreateSubscription()

  const handleClose = useCallback(() => {
    setEmail('')
    setValidationError(null)
    mutation.reset()
    onClose()
  }, [mutation, onClose])

  // Synchronize native HTML5 dialog element open state with isOpen prop
  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    if (isOpen) {
      if (typeof dialog.showModal === 'function' && !dialog.open) {
        dialog.showModal()
      }
      inputRef.current?.focus()
    } else if (typeof dialog.close === 'function' && dialog.open) {
      dialog.close()
    }
  }, [isOpen])

  // Global Escape key listener to ensure modal dismissal
  useEffect(() => {
    if (!isOpen) return

    const handleGlobalKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        handleClose()
      }
    }

    window.addEventListener('keydown', handleGlobalKeyDown)
    return () => window.removeEventListener('keydown', handleGlobalKeyDown)
  }, [isOpen, handleClose])




  if (!isOpen) {
    return null
  }

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setValidationError(null)

    const trimmedEmail = email.trim()
    if (!trimmedEmail) {
      setValidationError('A valid email address is required.')
      return
    }

    if (!RFC_EMAIL_REGEX.test(trimmedEmail)) {
      setValidationError('Please enter a valid email address.')
      return
    }

    mutation.mutate(
      {
        email: trimmedEmail,
        artistName: artistName.trim(),
        city: 'London',
      },
      {
        onSuccess: (data) => {
          onSubscribed?.(data)
          handleClose()
        },
      }
    )
  }


  const handleBackdropClick = (e: React.MouseEvent<HTMLDialogElement>) => {
    if (e.target === dialogRef.current) {
      handleClose()
    }
  }

  const errorMessage = validationError || mutation.error?.message


  return (
    <dialog
      ref={dialogRef}
      open={isOpen}
      onCancel={(e) => {
        e.preventDefault()
        handleClose()
      }}
      onClick={handleBackdropClick}
      aria-labelledby="track-modal-title"
      aria-describedby="track-modal-description"
      className="fixed inset-0 z-50 m-0 w-full h-full max-w-none max-h-none p-4 flex items-center justify-center bg-black/75 backdrop-blur-sm border-none overflow-y-auto"
    >
      <div
        className="relative w-full max-w-lg bg-[#22262d] border border-white/10 rounded-2xl p-6 sm:p-8 shadow-2xl space-y-6 text-[#f3f4f6]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-1">
            <h2 id="track-modal-title" className="text-xl font-bold text-white flex items-center gap-2">
              <span>🔔</span>
              <span>Track {artistName}</span>
            </h2>
            <p id="track-modal-description" className="text-sm text-gray-400">
              Get notified via email when new London shows or ticket releases are detected.
            </p>
          </div>
          <button
            type="button"
            onClick={handleClose}
            aria-label="Close modal"
            className="min-h-[44px] min-w-[44px] -mr-2 -mt-2 flex items-center justify-center text-gray-400 hover:text-white rounded-lg hover:bg-white/5 transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-400 cursor-pointer"
          >
            ✕
          </button>
        </div>

        {errorMessage && (
          <ErrorBanner message={errorMessage} />
        )}

        <form onSubmit={handleSubmit} className="space-y-5" noValidate>
          <div className="space-y-2">
            <label htmlFor="track-email-input" className="block text-sm font-medium text-gray-300">
              Email Address
            </label>
            <input
              ref={inputRef}
              id="track-email-input"
              type="email"
              value={email}
              onChange={(e) => {
                setEmail(e.target.value)
                if (validationError) setValidationError(null)
              }}
              placeholder="you@example.com"
              disabled={mutation.isPending}
              aria-invalid={Boolean(errorMessage)}
              className="w-full min-h-[44px] px-4 py-2.5 rounded-xl bg-[#181b1f] border border-white/10 text-white placeholder-gray-500 focus:outline-none focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 disabled:opacity-50 disabled:cursor-not-allowed text-sm transition-all"
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={handleClose}
              disabled={mutation.isPending}
              className="min-h-[44px] px-5 py-2.5 rounded-xl text-sm font-medium text-gray-300 hover:text-white hover:bg-white/5 transition-colors disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-white/30"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={mutation.isPending}
              className="min-h-[44px] px-6 py-2.5 rounded-xl text-sm font-semibold bg-emerald-500 hover:bg-emerald-400 active:bg-emerald-600 text-gray-950 flex items-center justify-center gap-2 transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-400 font-medium"
            >
              {mutation.isPending ? (
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
      </div>
    </dialog>
  )
}
