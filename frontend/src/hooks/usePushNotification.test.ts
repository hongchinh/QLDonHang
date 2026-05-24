import { renderHook, act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { usePushNotification } from './usePushNotification'

vi.mock('@/features/push/api', () => ({
  pushApi: {
    subscribe: vi.fn().mockResolvedValue(undefined),
    unsubscribe: vi.fn().mockResolvedValue(undefined),
  },
}))

// Stub browser APIs
function stubNotificationPermission(permission: NotificationPermission) {
  Object.defineProperty(Notification, 'permission', { value: permission, configurable: true })
}

function stubPushManager(subscribeResult: PushSubscription | null = null) {
  const mockPushManager = {
    getSubscription: vi.fn().mockResolvedValue(subscribeResult),
    subscribe: vi.fn().mockResolvedValue({
      endpoint: 'https://push.example.com/sub/1',
      toJSON: () => ({
        endpoint: 'https://push.example.com/sub/1',
        keys: { p256dh: 'key123', auth: 'auth123' },
      }),
    } as unknown as PushSubscription),
  }
  Object.defineProperty(navigator, 'serviceWorker', {
    value: {
      ready: Promise.resolve({
        pushManager: mockPushManager,
      }),
    },
    configurable: true,
  })
  return mockPushManager
}

describe('usePushNotification', () => {
  beforeEach(() => {
    Object.defineProperty(window, 'Notification', {
      value: { permission: 'default', requestPermission: vi.fn().mockResolvedValue('granted') },
      configurable: true,
    })
  })
  afterEach(() => vi.unstubAllGlobals())

  it('state là idle khi permission là default', () => {
    stubNotificationPermission('default')
    stubPushManager()
    const { result } = renderHook(() => usePushNotification('/vapid-key'))
    expect(result.current.state).toBe('idle')
  })

  it('state là denied khi Notification.permission là denied', () => {
    stubNotificationPermission('denied')
    const { result } = renderHook(() => usePushNotification('/vapid-key'))
    expect(result.current.state).toBe('denied')
  })

  it('unsupported khi không có Notification API', () => {
    const originalNotification = window.Notification
    // @ts-expect-error intentional
    delete window.Notification
    const { result } = renderHook(() => usePushNotification('/vapid-key'))
    expect(result.current.state).toBe('unsupported')
    window.Notification = originalNotification
  })

  it('subscribe chuyển state sang granted', async () => {
    stubNotificationPermission('default')
    stubPushManager()
    const { result } = renderHook(() => usePushNotification('BFakeVapidKey'))

    await act(async () => {
      await result.current.subscribe()
    })

    expect(result.current.state).toBe('granted')
  })

  it('unsubscribe chuyển state về idle và gọi pushApi.unsubscribe', async () => {
    const pushApiMod = await import('@/features/push/api')
    vi.mocked(pushApiMod.pushApi.unsubscribe).mockResolvedValue(undefined)

    // mockUnsubscribe also removes the sub from the mock manager so the
    // useEffect that re-runs on state==='idle' won't find it and flip back to 'granted'.
    const pushManager = stubPushManager(null)
    const mockUnsubscribeFn = vi.fn().mockImplementation(async () => {
      pushManager.getSubscription.mockResolvedValue(null)
      return true
    })
    const existingSub = {
      endpoint: 'https://push.example.com/sub/existing',
      unsubscribe: mockUnsubscribeFn,
    } as unknown as PushSubscription
    pushManager.getSubscription.mockResolvedValue(existingSub)

    stubNotificationPermission('granted')

    const { result } = renderHook(() => usePushNotification('BFakeVapidKey'))

    // Initial state is 'granted' since Notification.permission === 'granted'
    expect(result.current.state).toBe('granted')

    await act(async () => {
      await result.current.unsubscribe()
    })

    expect(result.current.state).toBe('idle')
    expect(mockUnsubscribeFn).toHaveBeenCalled()
    expect(pushApiMod.pushApi.unsubscribe).toHaveBeenCalledWith('https://push.example.com/sub/existing')
  })
})
