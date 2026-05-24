# Phase 01 — Backend SignalR

**Status:** [ ] pending
**Complexity:** M

## Objective

Thêm SignalR vào backend: hub, interface, implementation, cập nhật `NotificationService`, cấu hình `Program.cs`.

## Files

- `backend/src/OrderMgmt.Application/Notifications/Interfaces/IRealtimeNotifier.cs` (mới)
- `backend/src/OrderMgmt.WebApi/Hubs/NotificationHub.cs` (mới)
- `backend/src/OrderMgmt.WebApi/Hubs/SignalRNotifier.cs` (mới)
- `backend/src/OrderMgmt.Application/Notifications/Services/NotificationService.cs` (sửa)
- `backend/src/OrderMgmt.WebApi/Program.cs` (sửa)
- `backend/tests/OrderMgmt.UnitTests/Notifications/SignalRNotifierTests.cs` (mới — nếu project unit test tồn tại)

## Tasks

### Task 1 — Tạo `IRealtimeNotifier` interface

Không có test cho interface thuần — tạo file trực tiếp.

**Tạo file** `backend/src/OrderMgmt.Application/Notifications/Interfaces/IRealtimeNotifier.cs`:

```csharp
namespace OrderMgmt.Application.Notifications.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync(Guid userId, CancellationToken ct = default);
}
```

**Commit:**
```bash
git add backend/src/OrderMgmt.Application/Notifications/Interfaces/IRealtimeNotifier.cs
git commit -m "feat(signalr): add IRealtimeNotifier interface"
```

---

### Task 2 — Tạo `NotificationHub`

**Tạo thư mục** `backend/src/OrderMgmt.WebApi/Hubs/`.

**Tạo file** `backend/src/OrderMgmt.WebApi/Hubs/NotificationHub.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OrderMgmt.WebApi.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }
}
```

**Commit:**
```bash
git add backend/src/OrderMgmt.WebApi/Hubs/NotificationHub.cs
git commit -m "feat(signalr): add NotificationHub"
```

---

### Task 3 — Tạo `SignalRNotifier` và unit test

1. **Kiểm tra** project unit test có tồn tại không:

```bash
ls backend/tests/
```

Nếu có `OrderMgmt.UnitTests/`:
  - Tạo `backend/tests/OrderMgmt.UnitTests/Notifications/SignalRNotifierTests.cs`

Nếu không có project unit test, bỏ qua bước test — chỉ implement.

2. **Nếu có unit test project — viết failing test:**

```csharp
using Microsoft.AspNetCore.SignalR;
using Moq;
using OrderMgmt.WebApi.Hubs;
using Xunit;

namespace OrderMgmt.UnitTests.Notifications;

public class SignalRNotifierTests
{
    [Fact]
    public async Task NotifyUserAsync_SendsToCorrectGroup()
    {
        var userId = Guid.NewGuid();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group($"user-{userId}")).Returns(mockClientProxy.Object);

        var mockContext = new Mock<IHubContext<NotificationHub>>();
        mockContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var notifier = new SignalRNotifier(mockContext.Object);
        await notifier.NotifyUserAsync(userId);

        mockClientProxy.Verify(
            p => p.SendCoreAsync("NewNotification", Array.Empty<object>(), default),
            Times.Once);
    }
}
```

3. **Chạy test để verify FAIL:**

```bash
cd backend && dotnet test tests/OrderMgmt.UnitTests --filter "SignalRNotifier" --logger "console;verbosity=normal"
```

Expected: FAIL với `Cannot find type 'SignalRNotifier'`

4. **Tạo file** `backend/src/OrderMgmt.WebApi/Hubs/SignalRNotifier.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;
using OrderMgmt.Application.Notifications.Interfaces;

namespace OrderMgmt.WebApi.Hubs;

public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotifier(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task NotifyUserAsync(Guid userId, CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("NewNotification", cancellationToken: ct);
}
```

5. **Chạy test để verify PASS** (nếu có unit test project):

```bash
cd backend && dotnet test tests/OrderMgmt.UnitTests --filter "SignalRNotifier" --logger "console;verbosity=normal"
```

Expected: 1 test PASS.

6. **Commit:**

```bash
git add backend/src/OrderMgmt.WebApi/Hubs/SignalRNotifier.cs
# Nếu có unit test:
git add backend/tests/OrderMgmt.UnitTests/Notifications/SignalRNotifierTests.cs
git commit -m "feat(signalr): add SignalRNotifier implementation"
```

---

### Task 4 — Cập nhật `NotificationService` để inject `IRealtimeNotifier`

1. **Sửa** `backend/src/OrderMgmt.Application/Notifications/Services/NotificationService.cs`:

Thêm field và constructor parameter:

```csharp
private readonly IRealtimeNotifier _realtime;

public NotificationService(IAppDbContext db, IPushSender push, IRealtimeNotifier realtime)
{
    _db = db;
    _push = push;
    _realtime = realtime;
}
```

Trong `SendAsync`, sau dòng `await _push.SendAsync(...)`, thêm:

```csharp
await _realtime.NotifyUserAsync(userId, ct);
```

File sau khi sửa:

```csharp
using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Notifications.Interfaces;
using OrderMgmt.Application.Notifications.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Notifications;

namespace OrderMgmt.Application.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;
    private readonly IPushSender _push;
    private readonly IRealtimeNotifier _realtime;

    public NotificationService(IAppDbContext db, IPushSender push, IRealtimeNotifier realtime)
    {
        _db = db;
        _push = push;
        _realtime = realtime;
    }

    public async Task SendAsync(Guid userId, string type, string title, string? body, string? link, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Link = link,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _push.SendAsync(userId, title, body ?? string.Empty, link ?? "/", ct);
        await _realtime.NotifyUserAsync(userId, ct);
    }

    // ... các method còn lại giữ nguyên
}
```

2. **Verify build:**

```bash
cd backend && dotnet build src/OrderMgmt.Application
```

Expected: build thành công (chưa có DI registration nên integration test sẽ fail — sẽ fix ở Task 5).

---

### Task 5 — Cập nhật `Program.cs`

**Sửa** `backend/src/OrderMgmt.WebApi/Program.cs` — 4 thay đổi:

**a) Thêm using** ở đầu file:

```csharp
using OrderMgmt.Application.Notifications.Interfaces;
using OrderMgmt.WebApi.Hubs;
```

**b) Thêm `AddSignalR()`** sau `builder.Services.AddControllers()...` block:

```csharp
builder.Services.AddSignalR();
```

**c) Register `SignalRNotifier`** — thêm sau `builder.Services.AddScoped<ICurrentUser, CurrentUser>()`:

```csharp
builder.Services.AddScoped<IRealtimeNotifier, SignalRNotifier>();
```

**d) Thêm JWT `OnMessageReceived`** để SignalR đọc token từ query string — thêm **sau** block `AddOptions<JwtBearerOptions>` hiện tại (sau dòng đóng `});` của `.Configure<..>(...)`):

```csharp
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .PostConfigure(options =>
    {
        options.Events ??= new JwtBearerEvents();
        var existing = options.Events.OnMessageReceived;
        options.Events.OnMessageReceived = async ctx =>
        {
            if (existing is not null) await existing(ctx);
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) &&
                ctx.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
            {
                ctx.Token = token;
            }
        };
    });
```

**e) Map hub** — thêm sau `app.MapControllers()`:

```csharp
app.MapHub<NotificationHub>("/hubs/notifications");
```

**Verify build:**

```bash
cd backend && dotnet build src/OrderMgmt.WebApi
```

Expected: build thành công, không có error/warning mới.

**Chạy integration tests:**

```bash
cd backend && dotnet test tests/OrderMgmt.IntegrationTests --logger "console;verbosity=normal"
```

Expected: toàn bộ suite PASS (SignalRNotifier gửi tới group rỗng trong test — là no-op, không lỗi).

**Commit:**

```bash
git add backend/src/OrderMgmt.Application/Notifications/Services/NotificationService.cs
git add backend/src/OrderMgmt.WebApi/Program.cs
git commit -m "feat(signalr): wire SignalR into NotificationService and Program.cs"
```

---

## Verification

```bash
# Build
cd backend && dotnet build src/OrderMgmt.WebApi

# Toàn bộ integration test suite
cd backend && dotnet test tests/OrderMgmt.IntegrationTests --logger "console;verbosity=normal"
```

## Exit Criteria

- [ ] `dotnet build src/OrderMgmt.WebApi` — 0 errors
- [ ] Toàn bộ integration test suite PASS (không regression)
- [ ] `NotificationHub.cs`, `IRealtimeNotifier.cs`, `SignalRNotifier.cs` tồn tại
- [ ] `Program.cs` có `AddSignalR()`, `MapHub<NotificationHub>("/hubs/notifications")`, `PostConfigure` JWT events, `AddScoped<IRealtimeNotifier, SignalRNotifier>()`
