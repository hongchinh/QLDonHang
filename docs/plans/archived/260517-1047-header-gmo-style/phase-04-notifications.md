# Phase 04 — Notifications (BE + FE)

**Status:** [ ] pending
**Complexity:** L

## Objective
Replace `<HeaderNotificationsPlaceholder>` (từ phase 01) bằng notification thật: bảng DB `notifications` user-scoped, polling 60s từ FE, popover list, badge unread count, click item → navigate + auto mark-as-read. Phase này chỉ có CRUD endpoints + manual/seed; **domain event hook tự tạo notification (vd khi báo giá đổi trạng thái) out-of-scope**.

## Files

**Tạo mới**
- `backend/src/OrderMgmt.Domain/Notifications/Notification.cs`
- `backend/src/OrderMgmt.Application/Notifications/Interfaces/INotificationService.cs`
- `backend/src/OrderMgmt.Application/Notifications/Services/NotificationService.cs`
- `backend/src/OrderMgmt.Application/Notifications/Models/NotificationDto.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/NotificationsController.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/<timestamp>_AddNotifications.cs` (auto)
- `backend/tests/OrderMgmt.IntegrationTests/Notifications/NotificationsControllerTests.cs`
- `frontend/src/features/notifications/api.ts`
- `frontend/src/features/notifications/hooks.ts`
- `frontend/src/components/layout/header/header-notifications.tsx`

**Sửa**
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` — DbSet + Fluent config + (optional) seed
- `backend/src/OrderMgmt.WebApi/Program.cs` — DI service
- `frontend/src/components/layout/header/app-header.tsx` — thay placeholder bằng `<HeaderNotifications />`

**Xoá**
- `frontend/src/components/layout/header/header-notifications-placeholder.tsx`

## Tasks

1. Tạo entity `Notification`:
   ```csharp
   public sealed class Notification {
       public Guid Id { get; set; }
       public Guid UserId { get; set; }
       public string Type { get; set; } = default!;   // vd "QuotationCreated"
       public string Title { get; set; } = default!;  // ≤200
       public string? Body { get; set; }              // ≤1000
       public string? Link { get; set; }              // ≤500
       public bool IsRead { get; set; }
       public DateTimeOffset CreatedAt { get; set; }
   }
   ```

2. Cập nhật `AppDbContext`:
   - `DbSet<Notification> Notifications`.
   - Fluent config: table `notifications`, PK `Id`, max-length cho string fields, index composite `(UserId, IsRead, CreatedAt DESC)`.

3. Tạo migration:
   ```
   dotnet ef migrations add AddNotifications `
     -p backend/src/OrderMgmt.Infrastructure `
     -s backend/src/OrderMgmt.WebApi
   ```

4. Apply migration:
   ```
   dotnet ef database update `
     -p backend/src/OrderMgmt.Infrastructure `
     -s backend/src/OrderMgmt.WebApi
   ```

5. Tạo DTO:
   ```csharp
   public sealed record NotificationDto(
       Guid Id, string Type, string Title, string? Body, string? Link, bool IsRead, DateTimeOffset CreatedAt);
   ```

6. Tạo `INotificationService`:
   ```csharp
   Task<List<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, int limit, CancellationToken ct);
   Task<int> CountUnreadAsync(Guid userId, CancellationToken ct);
   Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct);
   Task MarkAllReadAsync(Guid userId, CancellationToken ct);
   ```

7. Implement `NotificationService` (DI `AppDbContext`):
   - `ListAsync`: query theo `UserId`, optional filter `IsRead=false`, order by `CreatedAt DESC`, take `limit` (default 10).
   - `CountUnreadAsync`: `COUNT WHERE UserId AND !IsRead`.
   - `MarkReadAsync`: tìm theo `Id`, **bắt buộc** check `entity.UserId == userId`; nếu không match → throw `NotFoundException` (BE pattern hiện có) để không leak existence.
   - `MarkAllReadAsync`: `UPDATE notifications SET IsRead=true WHERE UserId=@uid AND IsRead=false`.

8. Tạo `NotificationsController`:
   ```csharp
   [Authorize]
   public class NotificationsController : ApiControllerBase {
       private readonly INotificationService _svc;
       public NotificationsController(INotificationService svc) { _svc = svc; }

       [HttpGet("/api/notifications")]
       public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> List(
           [FromQuery] bool unreadOnly = false, [FromQuery] int limit = 10, CancellationToken ct = default)
           => Success(await _svc.ListAsync(CurrentUserId, unreadOnly, Math.Clamp(limit, 1, 50), ct));

       [HttpGet("/api/notifications/unread-count")]
       public async Task<ActionResult<ApiResponse<int>>> UnreadCount(CancellationToken ct)
           => Success(await _svc.CountUnreadAsync(CurrentUserId, ct));

       [HttpPost("/api/notifications/{id:guid}/read")]
       public async Task<ActionResult<ApiResponse>> MarkRead(Guid id, CancellationToken ct) {
           await _svc.MarkReadAsync(id, CurrentUserId, ct);
           return Success();
       }

       [HttpPost("/api/notifications/mark-all-read")]
       public async Task<ActionResult<ApiResponse>> MarkAllRead(CancellationToken ct) {
           await _svc.MarkAllReadAsync(CurrentUserId, ct);
           return Success();
       }
   }
   ```
   - `CurrentUserId` lấy theo pattern hiện có trong repo.

9. DI trong `Program.cs`:
   ```csharp
   services.AddScoped<INotificationService, NotificationService>();
   ```

10. Seed test data (chọn 1 cách):
    - **A.** Bổ sung vào `DbSeeder` 2-3 notification cho admin user khi seed dev DB.
    - **B.** Chạy SQL thủ công sau migration: `INSERT INTO notifications (...) VALUES (...);`.
    - Chọn A nếu repo đã có pattern seed; B nếu seed file phức tạp.

11. Tạo `frontend/src/features/notifications/api.ts`:
    - `listNotifications(unreadOnly?: boolean, limit?: number)` → GET.
    - `getUnreadCount()` → GET `/api/notifications/unread-count`.
    - `markRead(id: string)` → POST.
    - `markAllRead()` → POST.

12. Tạo `frontend/src/features/notifications/hooks.ts`:
    - `useUnreadCount()`: React Query, key `['notifications', 'unread-count']`, `refetchInterval: 60_000`, `refetchIntervalInBackground: false`, `staleTime: 30_000`.
    - `useNotifications(unreadOnly: boolean)`: React Query, key `['notifications', 'list', unreadOnly]`, `enabled: false` mặc định — caller `refetch()` khi mở popover.
    - `useMarkRead()`: useMutation, onSuccess invalidate cả `['notifications', 'unread-count']` và `['notifications', 'list']`.
    - `useMarkAllRead()`: useMutation, onSuccess tương tự.

13. Tạo `frontend/src/components/layout/header/header-notifications.tsx`:
    - Dùng `Popover` từ `@/components/ui/popover` (đã tạo ở phase 03).
    - Trigger: button vuông `Bell` icon + label "Thông báo" + badge unreadCount khi >0.
      - Badge: absolute top-right of icon, `bg-[hsl(var(--header-danger))] text-white text-xs rounded-full min-w-[18px] h-[18px] flex items-center justify-center`.
      - Hiển thị "9+" khi count > 9.
    - Khi popover mở: gọi `notifications.refetch()` để load list mới nhất.
    - Popover content (width ~360px):
      - Header: "Thông báo" + button "Đánh dấu tất cả đã đọc" (`useMarkAllRead`).
      - List: 10 notifications mới nhất, mỗi item:
        - Title (font-medium), Body (truncate 2 lines, text-muted), `formatDistanceToNow(createdAt, {locale: vi})`.
        - Style: nền `bg-blue-50` khi unread, trắng khi read.
        - Click: nếu `link` không null → `navigate(link)` + `markRead.mutate(id)`; nếu null → chỉ `markRead`.
      - Empty: "Chưa có thông báo".
      - Loading skeleton khi `isLoading`.
    - A11y: trigger `aria-label={`Thông báo, ${unreadCount} chưa đọc`}`.

14. Sửa `app-header.tsx`: import `HeaderNotifications` thay placeholder.

15. Xoá `header-notifications-placeholder.tsx`.

16. Integration test `NotificationsControllerTests` (pattern WebAppFactory như các test hiện có):
    - **Test 1**: `List` với 2 user (A, B), seed 2 notif cho A → A list chỉ thấy 2 của A, B list rỗng.
    - **Test 2**: `List?unreadOnly=true` chỉ trả notif chưa đọc.
    - **Test 3**: `CountUnread` trả đúng số.
    - **Test 4**: `MarkRead` của A trên notif của B → 404 (hoặc `NotFoundException`).
    - **Test 5**: `MarkRead` của A trên notif của A → `IsRead=true`.
    - **Test 6**: `MarkAllRead` → tất cả notif của user đó `IsRead=true`; notif của user khác không đổi.
    - ⚠️ Trước khi `dotnet test`: verify `TEST_DB_CONNECTION` ≠ dev WebApi DB (theo memory `feedback_test_db_separation_check.md`).

17. Run integration tests:
    ```
    dotnet test backend/tests/OrderMgmt.IntegrationTests `
      --filter "FullyQualifiedName~NotificationsControllerTests"
    ```

18. FE checks:
    ```
    cd frontend && npm run typecheck && npm run lint && npm run test
    ```

19. Manual test:
    - Seed 2 notif cho admin (1 có link `/quotations/<some-id>`, 1 không link).
    - Login admin → badge "2" hiện trên Bell icon.
    - Click Bell → popover hiện 2 item, item unread nền `bg-blue-50`.
    - Click item có link → navigate đến quotation + badge giảm còn "1" + popover close.
    - Reload → badge vẫn còn "1".
    - Mở Bell → click "Đánh dấu tất cả đã đọc" → badge biến mất.
    - Chờ 60s, mở DevTools Network → thấy poll request `/api/notifications/unread-count`.
    - Switch tab inactive (background) → React Query dừng poll; quay lại tab → resume.

## Verification

```
# Backend build
dotnet build backend/src/OrderMgmt.Domain backend/src/OrderMgmt.Application backend/src/OrderMgmt.Infrastructure backend/src/OrderMgmt.WebApi

# Integration tests (set TEST_DB_CONNECTION trước, khác dev DB)
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~NotificationsControllerTests"

# Frontend
cd frontend && npm run typecheck && npm run lint && npm run test
```

## Exit Criteria
- [ ] Migration `AddNotifications` applied; bảng `notifications` tồn tại với composite index (UserId, IsRead, CreatedAt DESC).
- [ ] `GET /api/notifications` trả notif của current user; không cross-user.
- [ ] `GET /api/notifications/unread-count` trả số chính xác.
- [ ] `POST /api/notifications/{id}/read` cập nhật `IsRead=true`; user khác mark notif không của mình → 404.
- [ ] `POST /api/notifications/mark-all-read` mark tất cả của current user, không đụng user khác.
- [ ] Integration tests 1-6 pass.
- [ ] FE badge hiện khi `unreadCount > 0`, "9+" khi >9, ẩn khi =0.
- [ ] FE: click item navigate (nếu có link) + auto mark-as-read (badge giảm trong cùng tick).
- [ ] FE: polling 60s thấy trên Network tab; pause khi tab inactive.
- [ ] "Đánh dấu tất cả đã đọc" clear badge ngay lập tức.
