# Phase 02 — Frontend

**Status:** [ ] pending
**Complexity:** S

## Objective

Cập nhật PWA manifest icons trong `vite.config.ts` để trỏ tới API endpoint thay vì file tĩnh, và cập nhật favicon trong `index.html`.

## Files

- `frontend/vite.config.ts`
- `frontend/index.html`

## Tasks

### Task 1 — Cập nhật manifest icons trong `vite.config.ts`

Mở `frontend/vite.config.ts`, thay thế block `icons`:

**Cũ:**
```ts
icons: [
  { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png' },
  { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png' },
  { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
],
```

**Mới:**
```ts
icons: [
  { src: '/api/settings/branding/icon/192', sizes: '192x192', type: 'image/png' },
  { src: '/api/settings/branding/icon/512', sizes: '512x512', type: 'image/png' },
  // Không dùng purpose: 'maskable' — maskable yêu cầu safe zone 60% ở giữa.
  // ResizeMode.Pad đơn giản không đảm bảo điều này; bỏ để tránh icon bị crop trên Android.
],
```

### Task 2 — Cập nhật favicon trong `index.html`

Mở `frontend/index.html`, thay thế thẻ favicon:

**Cũ:**
```html
<link rel="icon" type="image/svg+xml" href="/vite.svg" />
```

**Mới:**
```html
<link rel="icon" type="image/png" href="/api/settings/branding/icon/192" />
```

### Task 3 — Typecheck

```powershell
cd frontend
npm run typecheck
```

Expected: 0 errors.

### Task 4 — Build

```powershell
cd frontend
npm run build
```

Expected: build thành công, không có warning về icons.

### Task 5 — Commit

```powershell
git add frontend/vite.config.ts frontend/index.html
git commit -m "feat: update PWA manifest icons and favicon to use branding API"
```

## Verification

1. Chạy dev server: `npm run dev` và `dotnet run`
2. Mở `http://localhost:5173` trong Chrome
3. Chrome DevTools → Application → Manifest → Icons → kiểm tra 2 icon URLs hiển thị đúng và load được
4. Upload "Logo vuông" qua Settings → Cấu hình hệ thống → tab Branding
5. Hard-reload trang (`Ctrl+Shift+R`) → favicon trên tab browser đổi thành logo vừa upload
6. DevTools → Application → Manifest → refresh → icons cập nhật

## Exit Criteria

- `npm run typecheck` — 0 errors
- `npm run build` — thành công
- PWA manifest icons trỏ tới `/api/settings/branding/icon/192` và `/api/settings/branding/icon/512`
- Favicon trên tab browser load từ API (không còn Vite logo)
- Sau khi upload logo mới qua Settings, favicon cập nhật sau khi hard-reload (ETag cache 1 giờ)
