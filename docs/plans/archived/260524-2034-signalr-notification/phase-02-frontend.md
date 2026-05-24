# Phase 02 — Frontend Hook

**Status:** [ ] pending
**Complexity:** S

## Objective

Cài `@microsoft/signalr`, viết `useNotificationHub` hook, mount trong `AppLayout`.

## Files

- `frontend/package.json` (sửa — thêm @microsoft/signalr)
- `frontend/src/hooks/useNotificationHub.ts` (mới)
- `frontend/src/hooks/useNotificationHub.test.ts` (mới)
- `frontend/src/components/layout/app-layout.tsx` (sửa — mount hook)

## Tasks

### Task 1 — Cài `@microsoft/signalr`

```bash
cd frontend && npm install @microsoft/signalr
```

**Verify:**

```bash
node -e "require('@microsoft/signalr'); console.log('ok')" 2>/dev/null || node -e "import('@microsoft/signalr').then(() => console.log('ok'))"
```

Hoặc đơn giản hơn: kiểm tra `package.json` có `@microsoft/signalr` trong `dependencies`.

**Commit:**

```bash
git add frontend/package.json frontend/package-lock.json
git commit -m "chore: add @microsoft/signalr package"
```

---

### Task 2 — Viết failing test cho `useNotificationHub`

1. **Tạo file** `frontend/src/hooks/useNotificationHub.test.ts`:

```typescript
import { renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// Shared mocks — declared before vi.mock calls (hoisted by Vitest)
const mockOn = vi.fn()
const mockStart = vi.fn().mockResolvedValue(undefined)
const mockStop = vi.fn().mockResolvedValue(undefined)
const mockWithUrl = vi.fn().mockReturnThis()
const mockWithAutomaticReconnect = vi.fn().mockReturnThis()
const mockBuild = vi.fn().mockReturnValue({ on: mockOn, start: mockStart, stop: mockStop })
const invalidateQueries = vi.fn()

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(() => ({
    withUrl: mockWithUrl,
    withAutomaticReconnect: mockWithAutomaticReconnect,
    build: mockBuild,
  })),
}))

vi.mock('@/stores/auth-store', () => ({
  useAuthStore: vi.fn((selector: (s: { accessToken: string | null }) => unknown) =>
    selector({ accessToken: 'test-token' })
  ),
}))

vi.mock('@tanstack/react-query', () => ({
  useQueryClient: vi.fn(() => ({ invalidateQueries })),
}))

import { useNotificationHub } from './useNotificationHub'

describe('useNotificationHub', () => {
  beforeEach(() => vi.clearAllMocks())
  afterEach(() => vi.restoreAllMocks())

  it('connects to /hubs/notifications on mount', () => {
    renderHook(() => useNotificationHub())
    expect(mockWithUrl).toHaveBeenCalledWith(
      '/hubs/notifications',
      expect.objectContaining({ accessTokenFactory: expect.any(Function) })
    )
    expect(mockStart).toHaveBeenCalled()
  })

  it('registers NewNotification handler', () => {
    renderHook(() => useNotificationHub())
    expect(mockOn).toHaveBeenCalledWith('NewNotification', expect.any(Function))
  })

  it('stops connection on unmount', () => {
    const { unmount } = renderHook(() => useNotificationHub())
    unmount()
    expect(mockStop).toHaveBeenCalled()
  })

  it('invalidates unread-count query on NewNotification', () => {
    renderHook(() => useNotificationHub())

    // Lấy handler đã đăng ký với 'NewNotification' và gọi nó
    const handler = mockOn.mock.calls.find(([event]: [string]) => event === 'NewNotification')?.[1] as (() => void) | undefined
    handler?.()

    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: ['notifications', 'unread-count'],
    })
  })
})
```

2. **Chạy test để verify FAIL:**

```bash
cd frontend && npm run test -- useNotificationHub
```

Expected: FAIL với `Cannot find module './useNotificationHub'`

---

### Task 3 — Tạo `useNotificationHub` hook

**Tạo file** `frontend/src/hooks/useNotificationHub.ts`:

```typescript
import { useEffect } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@/stores/auth-store'
import { notificationsKeys } from '@/features/notifications/hooks'

export function useNotificationHub() {
  const queryClient = useQueryClient()
  const accessToken = useAuthStore((s) => s.accessToken)

  useEffect(() => {
    if (!accessToken) return

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('NewNotification', () => {
      queryClient.invalidateQueries({ queryKey: notificationsKeys.unreadCount })
    })

    connection.start().catch((err) => {
      console.error('SignalR connection failed:', err)
    })

    return () => {
      connection.stop()
    }
  }, [accessToken, queryClient])
}
```

**Lưu ý:** `accessToken` trong dependency array để hook reconnect sau khi token được refresh. `useAuthStore.getState()` (non-reactive) bên trong `accessTokenFactory` để lấy token mới nhất lúc reconnect mà không tạo ra vòng lặp.

---

### Task 4 — Chạy test để verify PASS

```bash
cd frontend && npm run test -- useNotificationHub
```

Expected: 4 tests PASS.

**Commit:**

```bash
git add frontend/src/hooks/useNotificationHub.ts frontend/src/hooks/useNotificationHub.test.ts
git commit -m "feat(signalr): add useNotificationHub hook"
```

---

### Task 5 — Mount `useNotificationHub` trong `AppLayout`

**Sửa** `frontend/src/components/layout/app-layout.tsx`:

1. Thêm import ở đầu file:

```tsx
import { useNotificationHub } from '@/hooks/useNotificationHub'
```

2. Thêm vào đầu function `AppLayout()`, sau các `const` hiện tại:

```tsx
useNotificationHub()
```

Ví dụ vị trí chèn (sau `const closeMobileDrawer = useUiStore(...)`):

```tsx
export function AppLayout() {
  const hasPermission = useAuthStore((s) => s.hasPermission);
  const isInRole = useAuthStore((s) => s.isInRole);
  const location = useLocation();

  const sidebarCollapsed = useUiStore((s) => s.sidebarCollapsed);
  const mobileDrawerOpen = useUiStore((s) => s.mobileDrawerOpen);
  const closeMobileDrawer = useUiStore((s) => s.closeMobileDrawer);

  useNotificationHub(); // ← thêm vào đây

  useEffect(() => {
    closeMobileDrawer();
  }, [location.pathname, closeMobileDrawer]);
  // ... rest unchanged
```

**Verify typecheck:**

```bash
cd frontend && npm run typecheck
```

Expected: 0 errors.

**Commit:**

```bash
git add frontend/src/components/layout/app-layout.tsx
git commit -m "feat(signalr): mount useNotificationHub in AppLayout"
```

---

### Task 6 — Verify toàn bộ

```bash
# Toàn bộ test suite
cd frontend && npm run test

# Typecheck
cd frontend && npm run typecheck

# Build
cd frontend && npm run build
```

Expected: tất cả PASS, build thành công.

---

## Verification

```bash
cd frontend && npm run test
cd frontend && npm run typecheck
cd frontend && npm run build
```

## Exit Criteria

- [ ] `@microsoft/signalr` trong `dependencies` của `package.json`
- [ ] `useNotificationHub.ts` tồn tại, 4 tests PASS
- [ ] `AppLayout` gọi `useNotificationHub()`
- [ ] `npm run typecheck` — 0 errors
- [ ] `npm run build` — thành công
- [ ] Toàn bộ test suite PASS (không regression)
