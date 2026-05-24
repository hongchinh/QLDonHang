# Phase 02 — API Runtime Cache

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm NetworkFirst caching cho tất cả `GET /api/**` requests vào Service Worker. App hiển thị dữ liệu cũ từ cache khi offline hoặc server timeout (8 giây). Cache expire sau 24 giờ.

## Files

- `frontend/src/sw.ts`

## Tasks

### Task 1 — Thêm NetworkFirst route vào sw.ts

Phase 1 chỉ có precache. Thêm route caching cho `/api/**`:

1. **Sửa** `frontend/src/sw.ts`:

```typescript
import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching'
import { registerRoute }                            from 'workbox-routing'
import { NetworkFirst }                             from 'workbox-strategies'
import { ExpirationPlugin }                         from 'workbox-expiration'

declare const self: ServiceWorkerGlobalScope

precacheAndRoute(self.__WB_MANIFEST)
cleanupOutdatedCaches()

// Matching theo pathname — hoạt động đúng khi API ở cùng origin (proxied).
// Nếu production deploy với API trên subdomain/domain khác, cần đổi thành
// match theo full URL: ({ url }) => url.hostname === 'api.yourdomain.com'
registerRoute(
  ({ url }) => url.pathname.startsWith('/api/'),
  new NetworkFirst({
    cacheName: 'api-cache',
    networkTimeoutSeconds: 8,
    plugins: [new ExpirationPlugin({ maxEntries: 100, maxAgeSeconds: 86400 })],
  }),
  'GET'
)

self.addEventListener('message', (event) => {
  if (event.data?.type === 'SKIP_WAITING') {
    self.skipWaiting()
  }
})
```

2. **Verify build:**

```bash
cd frontend && npm run build
```

Expected: build thành công.

3. **Commit:**

```bash
git add frontend/src/sw.ts
git commit -m "feat(pwa): add NetworkFirst API caching in Service Worker"
```

## Verification

SW caching không thể test bằng Vitest (cần browser environment thật). Verify thủ công:

```
1. npm run preview → mở http://localhost:4173, login, navigate vài trang
2. DevTools → Application → Cache Storage → api-cache → xác nhận các GET /api/* đã được cache
3. DevTools → Network → tick "Offline" → reload trang
   → App vẫn hiển thị dữ liệu (từ cache), không blank page
4. DevTools → Network → bỏ Offline, chờ 10 giây timeout trong 1 request
   → App fallback về cache trong vòng 8 giây
```

## Exit Criteria

- [ ] `npm run build` thành công
- [ ] DevTools → Cache Storage → `api-cache` có entries sau khi navigate
- [ ] Offline mode: app hiển thị dữ liệu từ cache thay vì error
