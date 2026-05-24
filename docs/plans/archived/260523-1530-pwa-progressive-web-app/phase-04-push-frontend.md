# Phase 04 — Push Notification Frontend

**Status:** [ ] pending
**Complexity:** M

## Objective

Xây dựng frontend cho push notifications: `usePushNotification` hook với full state machine (idle → loading → granted/denied/error), soft-prompt UX (app banner trước khi gọi `requestPermission`), và wire vào UI.

## Files

- `frontend/src/hooks/usePushNotification.ts` (mới)
- `frontend/src/hooks/usePushNotification.test.ts` (mới)
- `frontend/src/components/PushPermissionPrompt.tsx` (mới)
- `frontend/src/components/PushPermissionPrompt.test.tsx` (mới)
- `frontend/src/features/settings/` hoặc notification bell area (wire UI)

## Tasks

### Task 1 — Viết failing tests cho `usePushNotification`

1. **Tạo file** `frontend/src/hooks/usePushNotification.test.ts`:

```typescript
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
})
```

2. **Chạy test để verify FAIL:**

```bash
cd frontend && npm run test -- usePushNotification
```

Expected: FAIL với `Cannot find module './usePushNotification'`

3. **Tạo file** `frontend/src/features/push/api.ts` — API calls phải đi qua `apiClient` để có auth interceptor:

```typescript
import { apiClient } from '@/lib/api-client'

export const pushApi = {
  subscribe: (endpoint: string, p256dh: string, auth: string) =>
    apiClient.post('/api/push/subscribe', { endpoint, p256dh, auth }),

  unsubscribe: (endpoint: string) =>
    apiClient.delete('/api/push/subscribe', { data: { endpoint } }),
}
```

4. **Tạo file** `frontend/src/hooks/usePushNotification.ts`:

```typescript
import { useCallback, useEffect, useState } from 'react'
import { pushApi } from '@/features/push/api'

export type PushState = 'idle' | 'loading' | 'granted' | 'denied' | 'unsupported' | 'error'

export function usePushNotification(vapidPublicKey: string) {
  const [state, setState] = useState<PushState>(() => {
    if (!('Notification' in window)) return 'unsupported'
    if (Notification.permission === 'denied') return 'denied'
    if (Notification.permission === 'granted') return 'granted'
    return 'idle'
  })

  useEffect(() => {
    if (state !== 'idle') return
    if (!('serviceWorker' in navigator)) return

    navigator.serviceWorker.ready.then((reg) =>
      reg.pushManager.getSubscription()
    ).then((existing) => {
      if (existing) setState('granted')
    }).catch(() => {/* SW not ready yet — remain idle */})
  }, [state])

  const subscribe = useCallback(async () => {
    if (!('Notification' in window) || !('serviceWorker' in navigator)) {
      setState('unsupported')
      return
    }

    if (!vapidPublicKey) {
      setState('unsupported')
      return
    }

    setState('loading')
    try {
      const permission = await Notification.requestPermission()
      if (permission !== 'granted') {
        setState('denied')
        return
      }

      const reg = await navigator.serviceWorker.ready
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
      })

      const json = sub.toJSON()
      await pushApi.subscribe(json.endpoint!, json.keys!.p256dh, json.keys!.auth)

      setState('granted')
    } catch {
      setState('error')
    }
  }, [vapidPublicKey])

  const unsubscribe = useCallback(async () => {
    try {
      const reg = await navigator.serviceWorker.ready
      const sub = await reg.pushManager.getSubscription()
      if (!sub) return
      const endpoint = sub.endpoint
      await sub.unsubscribe()
      await pushApi.unsubscribe(endpoint)
      setState('idle')
    } catch {
      // Best-effort
    }
  }, [])

  return { state, subscribe, unsubscribe }
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const raw = atob(base64)
  return Uint8Array.from([...raw].map((c) => c.charCodeAt(0)))
}
```

5. **Chạy test để verify PASS:**

```bash
cd frontend && npm run test -- usePushNotification
```

Expected: 4 tests PASS.

6. **Commit:**

```bash
git add frontend/src/features/push/api.ts \
        frontend/src/hooks/usePushNotification.ts \
        frontend/src/hooks/usePushNotification.test.ts
git commit -m "feat(pwa): add usePushNotification hook with full state machine"
```

---

### Task 2 — Viết failing tests cho `PushPermissionPrompt`

1. **Tạo file** `frontend/src/components/PushPermissionPrompt.test.tsx`:

```typescript
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { PushPermissionPrompt } from './PushPermissionPrompt'
import type { PushState } from '@/hooks/usePushNotification'

describe('PushPermissionPrompt', () => {
  it('không render khi state bukan idle', () => {
    const { container } = render(
      <PushPermissionPrompt state="granted" onEnable={vi.fn()} onDismiss={vi.fn()} />
    )
    expect(container.firstChild).toBeNull()
  })

  it('render banner khi state là idle', () => {
    render(<PushPermissionPrompt state="idle" onEnable={vi.fn()} onDismiss={vi.fn()} />)
    expect(screen.getByRole('button', { name: /bật thông báo/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /để sau/i })).toBeInTheDocument()
  })

  it('render loading state khi state là loading', () => {
    render(<PushPermissionPrompt state="loading" onEnable={vi.fn()} onDismiss={vi.fn()} />)
    expect(screen.getByRole('button', { name: /bật thông báo/i })).toBeDisabled()
  })

  it('render error state với retry button', () => {
    render(<PushPermissionPrompt state="error" onEnable={vi.fn()} onDismiss={vi.fn()} />)
    expect(screen.getByRole('button', { name: /thử lại/i })).toBeInTheDocument()
  })

  it('không render khi state là denied', () => {
    const { container } = render(
      <PushPermissionPrompt state="denied" onEnable={vi.fn()} onDismiss={vi.fn()} />
    )
    expect(container.firstChild).toBeNull()
  })

  it('gọi onEnable khi click Bật thông báo', () => {
    const onEnable = vi.fn()
    render(<PushPermissionPrompt state="idle" onEnable={onEnable} onDismiss={vi.fn()} />)
    fireEvent.click(screen.getByRole('button', { name: /bật thông báo/i }))
    expect(onEnable).toHaveBeenCalledOnce()
  })

  it('gọi onDismiss khi click Để sau', () => {
    const onDismiss = vi.fn()
    render(<PushPermissionPrompt state="idle" onEnable={vi.fn()} onDismiss={onDismiss} />)
    fireEvent.click(screen.getByRole('button', { name: /để sau/i }))
    expect(onDismiss).toHaveBeenCalledOnce()
  })
})
```

2. **Chạy test để verify FAIL:**

```bash
cd frontend && npm run test -- PushPermissionPrompt
```

Expected: FAIL với `Cannot find module './PushPermissionPrompt'`

3. **Tạo file** `frontend/src/components/PushPermissionPrompt.tsx`:

```tsx
import type { PushState } from '@/hooks/usePushNotification'

interface Props {
  state: PushState
  onEnable(): void
  onDismiss(): void
}

export function PushPermissionPrompt({ state, onEnable, onDismiss }: Props) {
  if (state === 'denied' || state === 'granted' || state === 'unsupported') return null

  const isError = state === 'error'
  const isLoading = state === 'loading'

  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3">
      <div className="text-sm text-blue-900">
        {isError
          ? 'Bật thông báo thất bại. Vui lòng thử lại.'
          : 'Bật thông báo để nhận cập nhật khi trạng thái báo giá thay đổi.'}
      </div>
      <div className="flex shrink-0 gap-2">
        <button
          onClick={onEnable}
          disabled={isLoading}
          className="rounded bg-blue-700 px-3 py-1 text-sm font-semibold text-white hover:bg-blue-800 disabled:opacity-60"
        >
          {isLoading ? 'Đang xử lý...' : isError ? 'Thử lại' : 'Bật thông báo'}
        </button>
        {!isLoading && (
          <button
            onClick={onDismiss}
            className="rounded px-3 py-1 text-sm text-blue-600 hover:text-blue-800"
          >
            Để sau
          </button>
        )}
      </div>
    </div>
  )
}
```

4. **Chạy test để verify PASS:**

```bash
cd frontend && npm run test -- PushPermissionPrompt
```

Expected: 7 tests PASS.

5. **Commit:**

```bash
git add frontend/src/components/PushPermissionPrompt.tsx frontend/src/components/PushPermissionPrompt.test.tsx
git commit -m "feat(pwa): add PushPermissionPrompt soft-prompt component"
```

---

### Task 3 — Wire VAPID public key qua environment variable

VAPID public key cần được inject vào frontend tại build time qua Vite env:

1. **Tạo/sửa** `frontend/.env` (hoặc `frontend/.env.local` — không commit nếu chứa giá trị thật):

```env
VITE_VAPID_PUBLIC_KEY=
```

2. **Trong component/hook caller:**

```typescript
const vapidKey = import.meta.env.VITE_VAPID_PUBLIC_KEY as string
const { state, subscribe, unsubscribe } = usePushNotification(vapidKey)
```

3. **Thêm type declaration** vào `frontend/src/vite-env.d.ts`:

```typescript
interface ImportMetaEnv {
  readonly VITE_VAPID_PUBLIC_KEY: string
}
```

---

### Task 4 — Wire vào UI (notification bell hoặc settings area)

Tìm nơi phù hợp trong layout để đặt `PushPermissionPrompt`. Theo convention của project (`components/layout`), đặt trong layout sidebar hoặc header area phía dưới.

1. **Tìm** component header/sidebar hiện tại:

```bash
# Tìm layout component chứa notification bell
grep -r "NotificationsController\|unread-count\|notification" frontend/src --include="*.tsx" -l
```

2. **Trong component layout phù hợp**, thêm:

```tsx
import { usePushNotification } from '@/hooks/usePushNotification'
import { PushPermissionPrompt } from '@/components/PushPermissionPrompt'

const PUSH_DISMISS_KEY = 'push_prompt_dismissed_until'

const vapidKey = import.meta.env.VITE_VAPID_PUBLIC_KEY
const { state, subscribe } = usePushNotification(vapidKey)

const [dismissed, setDismissed] = useState(() => {
  const until = localStorage.getItem(PUSH_DISMISS_KEY)
  return until ? Date.now() < Number(until) : false
})

const handleDismiss = () => {
  const until = Date.now() + 30 * 24 * 60 * 60 * 1000 // 30 ngày
  localStorage.setItem(PUSH_DISMISS_KEY, String(until))
  setDismissed(true)
}

// Trong JSX (ví dụ: phía dưới notification bell):
{!dismissed && (
  <PushPermissionPrompt
    state={state}
    onEnable={subscribe}
    onDismiss={handleDismiss}
  />
)}
```

3. **Verify typecheck:**

```bash
cd frontend && npm run typecheck
```

4. **Commit:**

```bash
git add frontend/src/
git commit -m "feat(pwa): wire push notification prompt into layout"
```

---

## Verification

```bash
# Unit tests (tất cả)
cd frontend && npm run test

# Typecheck
cd frontend && npm run typecheck

# Build (xác nhận không lỗi)
cd frontend && npm run build
```

**Manual smoke test (yêu cầu VAPID keys đã được set):**

```
1. Set VITE_VAPID_PUBLIC_KEY trong .env với public key thật
2. Set Vapid keys trong backend user-secrets
3. npm run dev (frontend) + dotnet run (backend)
4. Mở app trong Chrome, login
5. Thấy PushPermissionPrompt banner
6. Click "Bật thông báo" → Chrome permission dialog hiện
7. Cho phép → banner ẩn, state = granted
8. Vào một báo giá, thay đổi status → Confirmed
9. Browser notification hiện trong vòng vài giây
10. Click notification → điều hướng đúng tới /quotations/{id}
```

## Exit Criteria

- [ ] `usePushNotification` tests — 4 tests PASS
- [ ] `PushPermissionPrompt` tests — 7 tests PASS
- [ ] `npm run typecheck` 0 errors
- [ ] `npm run build` thành công
- [ ] Toàn bộ test suite PASS (regression check)
- [ ] Manual: permission prompt hiện đúng flow (soft-prompt trước, browser dialog sau khi click Bật)
- [ ] Manual: state machine đúng — denied ẩn banner, granted ẩn banner, error hiện "Thử lại"
