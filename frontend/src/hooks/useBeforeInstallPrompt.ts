import { useCallback, useEffect, useState } from 'react'

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

const VISIT_KEY = 'pwa_visit_count'
const DISMISS_KEY = 'pwa_install_dismissed_until'
const MIN_VISITS = 2
const DISMISS_DAYS = 30

export function useBeforeInstallPrompt() {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null)

  const visitCount = (() => {
    const raw = localStorage.getItem(VISIT_KEY)
    return raw ? parseInt(raw, 10) : 0
  })()

  const isDismissed = (() => {
    const until = localStorage.getItem(DISMISS_KEY)
    return until ? Date.now() < Number(until) : false
  })()

  useEffect(() => {
    const count = visitCount + 1
    localStorage.setItem(VISIT_KEY, String(count))

    const handler = (e: Event) => {
      e.preventDefault()
      setDeferredPrompt(e as BeforeInstallPromptEvent)
    }
    window.addEventListener('beforeinstallprompt', handler)
    return () => window.removeEventListener('beforeinstallprompt', handler)
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const install = useCallback(async () => {
    if (!deferredPrompt) return
    await deferredPrompt.prompt()
    const { outcome } = await deferredPrompt.userChoice
    if (outcome === 'accepted') setDeferredPrompt(null)
  }, [deferredPrompt])

  const dismiss = useCallback(() => {
    const until = Date.now() + DISMISS_DAYS * 24 * 60 * 60 * 1000
    localStorage.setItem(DISMISS_KEY, String(until))
    setDeferredPrompt(null)
  }, [])

  const _simulateEvent = useCallback((e: BeforeInstallPromptEvent) => {
    setDeferredPrompt(e)
  }, [])

  const canShow = !!deferredPrompt && visitCount >= MIN_VISITS && !isDismissed

  return { canShow, install, dismiss, _simulateEvent }
}
