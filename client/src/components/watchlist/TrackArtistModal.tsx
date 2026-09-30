import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import { createSubscription } from '../../api/client'
import type { CreateSubscriptionRequest, SubscriptionResponse } from '../../api/types'
import { ErrorBanner } from '../common/ErrorBanner'
import { SubscriptionForm } from './SubscriptionForm'

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
  const [submittedEmail, setSubmittedEmail] = useState('')
  const [validationError, setValidationError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: (payload: CreateSubscriptionRequest) => createSubscription(payload),
  })
  const { reset: resetMutation } = mutation

  const handleClose = useCallback(() => {
    setEmail('')
    setSubmittedEmail('')
    setValidationError(null)
    resetMutation()
    onClose()
  }, [resetMutation, onClose])

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

    setSubmittedEmail(trimmedEmail)

    mutation.mutate(
      {
        email: trimmedEmail,
        artistName: artistName.trim(),
        city: 'London',
      },
      {
        onSuccess: (data) => {
          onSubscribed?.(data)
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

        {mutation.isSuccess ? (
          <div className="text-center space-y-5 py-2">
            <div
              aria-hidden="true"
              className="w-14 h-14 rounded-full bg-emerald-950/80 border border-emerald-500/40 flex items-center justify-center text-emerald-400 text-2xl mx-auto shadow-inner"
            >
              ✓
            </div>
            <div className="space-y-2">
              <h3 className="text-lg font-bold text-white">
                You're tracking {artistName}!
              </h3>
              <p className="text-sm text-gray-300 max-w-sm mx-auto leading-relaxed">
                We'll email <span className="text-emerald-300 font-semibold">{submittedEmail}</span> when new London shows or ticket drops are announced.
              </p>
            </div>
            <div className="pt-2">
              <button
                type="button"
                onClick={handleClose}
                autoFocus
                className="w-full min-h-[44px] px-6 py-2.5 rounded-xl text-sm font-semibold bg-emerald-500 hover:bg-emerald-400 active:bg-emerald-600 text-gray-950 transition-all cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-400"
              >
                Done
              </button>
            </div>
          </div>
        ) : (
          <>
            {errorMessage && <ErrorBanner message={errorMessage} />}

            <SubscriptionForm
              email={email}
              onChangeEmail={(val) => {
                setEmail(val)
                if (validationError) setValidationError(null)
              }}
              onSubmit={handleSubmit}
              onCancel={handleClose}
              isPending={mutation.isPending}
              hasError={Boolean(errorMessage)}
              inputRef={inputRef}
            />
          </>
        )}
      </div>
    </dialog>
  )
}
