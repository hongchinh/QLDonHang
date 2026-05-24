# Phase 01 — Installable + Static Precache

**Status:** [ ] pending
**Complexity:** L

## Objective

Biến app thành PWA installable: Web App Manifest + Service Worker với precache shell HTML/JS/CSS. Đồng thời bổ sung `InstallPrompt` banner (dùng `beforeinstallprompt`) và update toast (dùng `useRegisterSW` với `registerType: 'prompt'`). Không thay đổi backend.

## Files

- `frontend/package.json`
- `frontend/vite.config.ts`
- `frontend/vitest.config.ts` (mới)
- `frontend/src/sw.ts` (mới)
- `frontend/public/icons/icon-192.png` (mới — placeholder)
- `frontend/public/icons/icon-512.png` (mới — placeholder)
- `frontend/src/hooks/useBeforeInstallPrompt.ts` (mới)
- `frontend/src/components/InstallPrompt.tsx` (mới)
- `frontend/src/components/InstallPrompt.test.tsx` (mới)
- `frontend/src/hooks/useBeforeInstallPrompt.test.ts` (mới)
- `frontend/src/App.tsx` (sửa — mount InstallPrompt + update toast)

## Tasks

### Task 1 — Split vitest.config.ts và verify tests pass

Mục đích: tách test config ra khỏi `vite.config.ts` để `vite.config.ts` có thể import từ `'vite'` thay vì `'vitest/config'`.

1. **Tạo file mới** `frontend/vitest.config.ts`:

```typescript
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import path from 'node:path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
```

2. **Sửa** `frontend/vite.config.ts`: đổi `import { defineConfig } from 'vitest/config'` thành `import { defineConfig } from 'vite'` và xóa toàn bộ block `test: { ... }`:

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

const vendorChunks: Record<string, string> = {
  'react/': 'react',
  'react-dom/': 'react',
  'react-router': 'react',
  '@tanstack/': 'query',
  '@radix-ui/': 'radix',
  'react-hook-form': 'forms',
  '@hookform/': 'forms',
  zod: 'forms',
}

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5050',
        changeOrigin: true,
      },
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: (id) => {
          if (!id.includes('node_modules')) return undefined
          for (const [needle, chunk] of Object.entries(vendorChunks)) {
            if (id.includes(needle)) return chunk
          }
          return undefined
        },
      },
    },
  },
})
```

3. **Sửa** `frontend/package.json`: cập nhật script `test` để dùng `vitest.config.ts`:

```json
"test": "vitest run --config vitest.config.ts",
"test:watch": "vitest --config vitest.config.ts",
"test:ui": "vitest --ui --config vitest.config.ts"
```

4. **Chạy test để verify không bị break:**

```bash
cd frontend && npm run test
```

Expected: tất cả existing tests PASS (cùng kết quả như trước split).

5. **Commit:**

```bash
git add frontend/vite.config.ts frontend/vitest.config.ts frontend/package.json
git commit -m "chore: split vitest.config.ts from vite.config.ts for VitePWA compatibility"
```

---

### Task 2 — Cài đặt packages

1. **Cài packages:**

```bash
cd frontend
npm install --save-dev vite-plugin-pwa workbox-core workbox-precaching workbox-routing workbox-strategies workbox-expiration
```

2. **Verify** `package.json`: tất cả 6 packages trong `devDependencies` (workbox được bundle vào `sw.js` tại build time, không phải runtime dep của app).

3. **Commit:**

```bash
git add frontend/package.json frontend/package-lock.json
git commit -m "chore: add vite-plugin-pwa and workbox packages"
```

---

### Task 3 — Tạo placeholder icons

Icons thực cần designer cung cấp. Tạo placeholder PNG bằng Node.js script inline:

```bash
cd frontend
node -e "
const { createCanvas } = require('canvas');
// Nếu không có 'canvas' package, dùng cách khác
"
```

Thay thế: dùng cách đơn giản hơn — download placeholder từ placehold.co hoặc tạo file PNG tối giản bằng buffer cứng.

**Cách thực tế nhất:** copy bất kỳ PNG 192×192 và 512×512 có sẵn trong project vào `public/icons/`, hoặc dùng lệnh `magick` nếu ImageMagick có sẵn:

```bash
# Nếu có ImageMagick:
magick -size 192x192 xc:#1e40af -fill white -gravity center \
  -font Arial -pointsize 24 -annotate 0 "QLĐơnHàng" \
  frontend/public/icons/icon-192.png

magick -size 512x512 xc:#1e40af -fill white -gravity center \
  -font Arial -pointsize 64 -annotate 0 "QLĐơnHàng" \
  frontend/public/icons/icon-512.png

# Nếu không có ImageMagick, tạo thư mục và dùng placeholder online:
mkdir -p frontend/public/icons
# Copy từ bất kỳ nguồn nào, chú ý kích thước đúng
```

**Sau khi có 2 file PNG:** verify bằng cách mở file trong trình xem ảnh để đảm bảo không corrupt.

```bash
git add frontend/public/icons/
git commit -m "chore: add placeholder app icons (192x192 and 512x512)"
```

---

### Task 4 — Tạo Service Worker (`src/sw.ts`)

Service Worker Phase 1 chỉ cần precache. Route API cache sẽ thêm ở Phase 2.

1. **Tạo file** `frontend/src/sw.ts`:

```typescript
import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching'

declare const self: ServiceWorkerGlobalScope

precacheAndRoute(self.__WB_MANIFEST)
cleanupOutdatedCaches()

self.addEventListener('message', (event) => {
  if (event.data?.type === 'SKIP_WAITING') {
    self.skipWaiting()
  }
})
```

2. **Thêm type declaration** cho `__WB_MANIFEST`. Tạo hoặc cập nhật `frontend/src/vite-env.d.ts`:

```typescript
/// <reference types="vite/client" />
/// <reference types="vite-plugin-pwa/client" />
```

`vite-plugin-pwa/client` đã declare `__WB_MANIFEST` — không cần thêm `declare module 'workbox-precaching'` (sẽ override types hiện có và gây conflict).

---

### Task 5 — Cấu hình VitePWA trong vite.config.ts

1. **Sửa** `frontend/vite.config.ts` — thêm VitePWA import và plugin:

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import { VitePWA } from 'vite-plugin-pwa'

const vendorChunks: Record<string, string> = {
  'react/': 'react',
  'react-dom/': 'react',
  'react-router': 'react',
  '@tanstack/': 'query',
  '@radix-ui/': 'radix',
  'react-hook-form': 'forms',
  '@hookform/': 'forms',
  zod: 'forms',
}

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'prompt',
      strategies: 'injectManifest',
      injectManifest: { swSrc: 'src/sw.ts' },
      manifest: {
        name: 'QL Đơn Hàng',
        short_name: 'QLĐơnHàng',
        description: 'Quản lý báo giá, khách hàng, hàng hóa',
        display: 'standalone',
        theme_color: '#1e40af',
        background_color: '#ffffff',
        start_url: '/',
        icons: [
          { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      devOptions: {
        enabled: true,
        type: 'module',
      },
    }),
  ],
  resolve: { alias: { '@': path.resolve(__dirname, './src') } },
  server: {
    port: 5173,
    proxy: { '/api': { target: 'http://localhost:5050', changeOrigin: true } },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: (id) => {
          if (!id.includes('node_modules')) return undefined
          for (const [needle, chunk] of Object.entries(vendorChunks)) {
            if (id.includes(needle)) return chunk
          }
          return undefined
        },
      },
    },
  },
})
```

2. **Verify build không lỗi:**

```bash
cd frontend && npm run build
```

Expected: build thành công, `dist/sw.js` được tạo, `dist/manifest.webmanifest` tồn tại.

3. **Commit:**

```bash
git add frontend/vite.config.ts frontend/src/sw.ts frontend/src/vite-env.d.ts
git commit -m "feat(pwa): add Service Worker with precache and VitePWA config"
```

---

### Task 6 — Viết hook `useBeforeInstallPrompt` và test

1. **Viết failing test** trước — tạo `frontend/src/hooks/useBeforeInstallPrompt.test.ts`:

```typescript
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
```

2. **Chạy test để verify FAIL:**

```bash
cd frontend && npm run test -- useBeforeInstallPrompt
```

Expected: FAIL với `Cannot find module './useBeforeInstallPrompt'`

3. **Tạo file** `frontend/src/hooks/useBeforeInstallPrompt.ts`:

```typescript
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
```

4. **Chạy test để verify PASS:**

```bash
cd frontend && npm run test -- useBeforeInstallPrompt
```

Expected: 4 tests PASS.

5. **Commit:**

```bash
git add frontend/src/hooks/useBeforeInstallPrompt.ts frontend/src/hooks/useBeforeInstallPrompt.test.ts
git commit -m "feat(pwa): add useBeforeInstallPrompt hook with visit counter and dismiss logic"
```

---

### Task 7 — Tạo `InstallPrompt.tsx` và test

1. **Viết failing test** — tạo `frontend/src/components/InstallPrompt.test.tsx`:

```typescript
import { render, screen, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { InstallPrompt } from './InstallPrompt'

describe('InstallPrompt', () => {
  it('không render khi canShow = false', () => {
    const { container } = render(
      <InstallPrompt canShow={false} onInstall={vi.fn()} onDismiss={vi.fn()} />
    )
    expect(container.firstChild).toBeNull()
  })

  it('render banner khi canShow = true', () => {
    render(<InstallPrompt canShow={true} onInstall={vi.fn()} onDismiss={vi.fn()} />)
    expect(screen.getByRole('button', { name: /cài đặt/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /không/i })).toBeInTheDocument()
  })

  it('gọi onInstall khi click Cài đặt', () => {
    const onInstall = vi.fn()
    render(<InstallPrompt canShow={true} onInstall={onInstall} onDismiss={vi.fn()} />)
    fireEvent.click(screen.getByRole('button', { name: /cài đặt/i }))
    expect(onInstall).toHaveBeenCalledOnce()
  })

  it('gọi onDismiss khi click Không', () => {
    const onDismiss = vi.fn()
    render(<InstallPrompt canShow={true} onInstall={vi.fn()} onDismiss={onDismiss} />)
    fireEvent.click(screen.getByRole('button', { name: /không/i }))
    expect(onDismiss).toHaveBeenCalledOnce()
  })
})
```

2. **Chạy test để verify FAIL:**

```bash
cd frontend && npm run test -- InstallPrompt
```

Expected: FAIL với `Cannot find module './InstallPrompt'`

3. **Tạo file** `frontend/src/components/InstallPrompt.tsx`:

```tsx
interface Props {
  canShow: boolean
  onInstall(): void
  onDismiss(): void
}

export function InstallPrompt({ canShow, onInstall, onDismiss }: Props) {
  if (!canShow) return null
  return (
    <div className="fixed top-0 inset-x-0 z-50 flex items-center justify-between gap-4 bg-blue-800 px-4 py-3 text-white shadow-md">
      <span className="text-sm font-medium">Cài đặt QL Đơn Hàng để truy cập nhanh hơn</span>
      <div className="flex shrink-0 gap-2">
        <button
          onClick={onInstall}
          className="rounded bg-white px-3 py-1 text-sm font-semibold text-blue-800 hover:bg-blue-50"
        >
          Cài đặt
        </button>
        <button
          onClick={onDismiss}
          className="rounded px-3 py-1 text-sm text-blue-200 hover:text-white"
        >
          Không, cảm ơn
        </button>
      </div>
    </div>
  )
}
```

4. **Chạy test để verify PASS:**

```bash
cd frontend && npm run test -- InstallPrompt
```

Expected: 4 tests PASS.

5. **Commit:**

```bash
git add frontend/src/components/InstallPrompt.tsx frontend/src/components/InstallPrompt.test.tsx
git commit -m "feat(pwa): add InstallPrompt banner component"
```

---

### Task 8 — Mount InstallPrompt và update toast trong App.tsx

1. **Tìm** file `frontend/src/App.tsx` (hoặc root layout component) và thêm:

```tsx
import { useRegisterSW } from 'virtual:pwa-register/react'
import { useBeforeInstallPrompt } from '@/hooks/useBeforeInstallPrompt'
import { InstallPrompt } from '@/components/InstallPrompt'

// Trong component App:
const { canShow, install, dismiss } = useBeforeInstallPrompt()

const {
  needRefresh: [needRefresh, setNeedRefresh],
  updateServiceWorker,
} = useRegisterSW({ onRegisteredSW(_, sw) { /* optional logging */ } })

// JSX: thêm vào đầu return
<>
  <InstallPrompt canShow={canShow} onInstall={install} onDismiss={dismiss} />
  {needRefresh && (
    <div className="fixed bottom-4 right-4 z-50 flex items-center gap-3 rounded-lg bg-gray-900 px-4 py-3 text-sm text-white shadow-xl">
      <span>Có phiên bản mới</span>
      <button
        onClick={() => updateServiceWorker(true)}
        className="rounded bg-blue-500 px-3 py-1 text-xs font-semibold hover:bg-blue-400"
      >
        Cập nhật ngay
      </button>
      <button onClick={() => setNeedRefresh(false)} aria-label="Đóng" className="text-gray-400 hover:text-white">✕</button>
    </div>
  )}
  {/* ... rest of app */}
</>
```

2. **Thêm type declaration** cho `virtual:pwa-register/react` nếu TypeScript báo lỗi. File `frontend/src/vite-env.d.ts` đã có `/// <reference types="vite-plugin-pwa/client" />` từ Task 4 — đủ để resolve type.

3. **Verify typecheck:**

```bash
cd frontend && npm run typecheck
```

Expected: không có lỗi TypeScript.

4. **Commit:**

```bash
git add frontend/src/App.tsx frontend/src/vite-env.d.ts
git commit -m "feat(pwa): mount InstallPrompt and SW update toast in App"
```

---

## Verification

```bash
# Unit tests
cd frontend && npm run test

# Typecheck
cd frontend && npm run typecheck

# Build (verify SW compiles)
cd frontend && npm run build
# → dist/sw.js tồn tại
# → dist/manifest.webmanifest tồn tại

# Preview + Lighthouse
cd frontend && npm run preview
# Mở http://localhost:4173 trong Chrome
# DevTools → Application → Manifest → verify name/icons/display
# DevTools → Application → Service Workers → verify registered
# Lighthouse → PWA → score >= 80
```

## Exit Criteria

- [ ] `npm run test` PASS (không có test nào fail so với trước phase)
- [ ] `npm run typecheck` 0 errors
- [ ] `npm run build` thành công, tạo ra `dist/sw.js` và `dist/manifest.webmanifest`
- [ ] Chrome DevTools → Application → Manifest hiển thị đúng name, icons, display: standalone
- [ ] Chrome DevTools → Application → Service Workers: status "activated and running"
- [ ] Chrome address bar hiển thị nút install (sau khi đã visit >= 2 lần)
