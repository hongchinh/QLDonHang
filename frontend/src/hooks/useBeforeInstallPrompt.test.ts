import { renderHook, act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useBeforeInstallPrompt } from './useBeforeInstallPrompt'

const VISIT_KEY = 'pwa_visit_count'
const DISMISS_KEY = 'pwa_install_dismissed_until'

describe('useBeforeInstallPrompt', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.stubGlobal('addEventListener', vi.fn())
    vi.stubGlobal('removeEventListener', vi.fn())
  })
  afterEach(() => vi.unstubAllGlobals())

  it('không hiện prompt khi visit < 2', () => {
    localStorage.setItem(VISIT_KEY, '1')
    const { result } = renderHook(() => useBeforeInstallPrompt())
    expect(result.current.canShow).toBe(false)
  })

  it('hiện prompt khi visit >= 2 và có deferredPrompt', async () => {
    localStorage.setItem(VISIT_KEY, '2')
    const { result } = renderHook(() => useBeforeInstallPrompt())

    const fakeEvent = { preventDefault: vi.fn(), prompt: vi.fn(), userChoice: Promise.resolve({ outcome: 'accepted' }) }
    act(() => result.current._simulateEvent(fakeEvent as unknown as BeforeInstallPromptEvent))

    expect(result.current.canShow).toBe(true)
  })

  it('không hiện khi đang trong dismiss window', () => {
    localStorage.setItem(VISIT_KEY, '3')
    localStorage.setItem(DISMISS_KEY, String(Date.now() + 86400_000))
    const { result } = renderHook(() => useBeforeInstallPrompt())
    expect(result.current.canShow).toBe(false)
  })

  it('dismiss lưu timestamp 30 ngày vào localStorage', () => {
    localStorage.setItem(VISIT_KEY, '3')
    const { result } = renderHook(() => useBeforeInstallPrompt())
    act(() => result.current.dismiss())
    const until = Number(localStorage.getItem(DISMISS_KEY))
    expect(until).toBeGreaterThan(Date.now() + 29 * 24 * 60 * 60 * 1000)
  })
})
