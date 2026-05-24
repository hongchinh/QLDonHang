# Phase 03 — Push Notification Backend

**Status:** [ ] pending
**Complexity:** L

## Objective

Xây dựng toàn bộ backend cho push notifications:
- `PushSubscription` entity (Domain) — lưu subscription của từng device
- `IPushSender` interface (Application) + `PushSenderService` (Infrastructure) — gửi push qua WebPush NuGet, xử lý 410 cleanup
- Mở rộng `INotificationService.SendAsync` — persist in-app Notification + dispatch push
- `PushSubscriptionController` — `POST` (upsert) + `DELETE` (theo endpoint)
- Tích hợp vào `QuotationService.TransitionAsync` — gửi notification khi `Confirmed`, `AccountingConfirmed`, `Cancelled`
- EF migration

## Files

- `backend/src/OrderMgmt.Domain/Notifications/PushSubscription.cs` (mới)
- `backend/src/OrderMgmt.Application/Notifications/Interfaces/IPushSender.cs` (mới)
- `backend/src/OrderMgmt.Application/Notifications/Interfaces/INotificationService.cs` (sửa)
- `backend/src/OrderMgmt.Application/Notifications/Services/NotificationService.cs` (sửa)
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` (sửa)
- `backend/src/OrderMgmt.Infrastructure/Notifications/VapidOptions.cs` (mới)
- `backend/src/OrderMgmt.Infrastructure/Notifications/PushSenderService.cs` (mới)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/PushSubscriptionConfiguration.cs` (mới)
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` (sửa)
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` (sửa)
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` (không sửa — IPushSender là infra concern)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (sửa)
- `backend/src/OrderMgmt.WebApi/Controllers/PushSubscriptionController.cs` (mới)
- `backend/src/OrderMgmt.WebApi/appsettings.json` (sửa)
- `backend/tests/OrderMgmt.IntegrationTests/Fixtures/WebAppFactory.cs` (sửa)
- `backend/tests/OrderMgmt.IntegrationTests/Push/PushSubscriptionTests.cs` (mới)
- EF migration (generate bằng CLI)

## Tasks

### Task 1 — Viết failing integration tests

1. **Tạo thư mục** `backend/tests/OrderMgmt.IntegrationTests/Push/`.

2. **Tạo file** `backend/tests/OrderMgmt.IntegrationTests/Push/PushSubscriptionTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Push;

[Collection(nameof(PostgresCollection))]
public class PushSubscriptionTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public PushSubscriptionTests(PostgresFixture pg)
    {
        _factory = pg.Factory;
        _client = pg.AdminClient;
    }

    private static object ValidSubscriptionBody(string endpoint = "https://push.example.com/sub/123") => new
    {
        endpoint,
        p256dh = "BNbxSuT_abc123",
        auth = "auth_token_xyz"
    };

    [Fact]
    public async Task Subscribe_CreatesSubscription()
    {
        var resp = await _client.PostAsJsonAsync("/api/push/subscribe", ValidSubscriptionBody());
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(
            s => s.Endpoint == "https://push.example.com/sub/123");
        sub.Should().NotBeNull();
    }

    [Fact]
    public async Task Subscribe_SameEndpoint_Upserts()
    {
        var body = ValidSubscriptionBody("https://push.example.com/sub/upsert");
        await _client.PostAsJsonAsync("/api/push/subscribe", body);
        await _client.PostAsJsonAsync("/api/push/subscribe", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.PushSubscriptions.CountAsync(
            s => s.Endpoint == "https://push.example.com/sub/upsert");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Unsubscribe_DeletesSubscription()
    {
        var endpoint = "https://push.example.com/sub/delete";
        await _client.PostAsJsonAsync("/api/push/subscribe", ValidSubscriptionBody(endpoint));

        var resp = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/push/subscribe")
        {
            Content = JsonContent.Create(new { endpoint })
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        sub.Should().BeNull();
    }

    [Fact]
    public async Task Unsubscribe_NonExistent_ReturnsOk()
    {
        var resp = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/push/subscribe")
        {
            Content = JsonContent.Create(new { endpoint = "https://push.example.com/nonexistent" })
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

3. **Chạy test để verify FAIL:**

```bash
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "PushSubscription" --logger "console;verbosity=normal"
```

Expected: FAIL — `AppDbContext` không có `PushSubscriptions`, endpoint chưa tồn tại.

---

### Task 2 — PushSubscription entity (Domain)

**Tạo file** `backend/src/OrderMgmt.Domain/Notifications/PushSubscription.cs`:

```csharp
namespace OrderMgmt.Domain.Notifications;

public class PushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = default!;
    public string P256DH { get; set; } = default!;
    public string Auth { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

---

### Task 3 — IPushSender interface (Application)

**Tạo file** `backend/src/OrderMgmt.Application/Notifications/Interfaces/IPushSender.cs`:

```csharp
namespace OrderMgmt.Application.Notifications.Interfaces;

public interface IPushSender
{
    Task SendAsync(Guid userId, string title, string body, string url, CancellationToken ct = default);
}
```

---

### Task 4 — Mở rộng INotificationService (Application)

**Sửa** `backend/src/OrderMgmt.Application/Notifications/Interfaces/INotificationService.cs` — thêm `SendAsync`:

```csharp
using OrderMgmt.Application.Notifications.Models;

namespace OrderMgmt.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid userId, string type, string title, string? body, string? link, CancellationToken ct = default);
    Task<List<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, int limit, CancellationToken ct = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);
    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
```

---

### Task 5 — Mở rộng IAppDbContext (Application)

**Sửa** `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` — thêm `DbSet<PushSubscription>`:

```csharp
// Thêm using
using OrderMgmt.Domain.Notifications;

// Thêm vào interface sau DbSet<Notification>:
DbSet<PushSubscription> PushSubscriptions { get; }
```

---

### Task 6 — VapidOptions (Infrastructure)

**Tạo file** `backend/src/OrderMgmt.Infrastructure/Notifications/VapidOptions.cs`:

```csharp
namespace OrderMgmt.Infrastructure.Notifications;

public class VapidOptions
{
    public const string SectionName = "Vapid";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
```

---

### Task 7 — PushSenderService (Infrastructure)

**Tạo file** `backend/src/OrderMgmt.Infrastructure/Notifications/PushSenderService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Notifications.Interfaces;
using WebPush;

namespace OrderMgmt.Infrastructure.Notifications;

public class PushSenderService : IPushSender
{
    private readonly IAppDbContext _db;
    private readonly IOptions<VapidOptions> _vapid;
    private readonly ILogger<PushSenderService> _logger;

    public PushSenderService(IAppDbContext db, IOptions<VapidOptions> vapid, ILogger<PushSenderService> logger)
    {
        _db = db;
        _vapid = vapid;
        _logger = logger;
    }

    public async Task SendAsync(Guid userId, string title, string body, string url, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_vapid.Value.PrivateKey)) return;

        var subscriptions = await _db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0) return;

        var client = new WebPushClient();
        client.SetVapidDetails(
            _vapid.Value.Subject,
            _vapid.Value.PublicKey,
            _vapid.Value.PrivateKey);

        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body, url });

        var toDelete = new List<Domain.Notifications.PushSubscription>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload, ct);
            }
            catch (WebPushException ex) when ((int)ex.StatusCode == 410)
            {
                toDelete.Add(sub);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send push notification to endpoint {Endpoint}", sub.Endpoint);
            }
        }

        if (toDelete.Count > 0)
        {
            _db.PushSubscriptions.RemoveRange(toDelete);
            await _db.SaveChangesAsync(ct);
        }
    }
}
```

---

### Task 8 — Implement NotificationService.SendAsync (Application)

**Sửa** `backend/src/OrderMgmt.Application/Notifications/Services/NotificationService.cs` — thêm constructor với `IPushSender` và implement `SendAsync`:

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
    private readonly IDateTime _clock;
    private readonly IPushSender _push;

    public NotificationService(IAppDbContext db, IDateTime clock, IPushSender push)
    {
        _db = db;
        _clock = clock;
        _push = push;
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
            CreatedAt = _clock.UtcNow,
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _push.SendAsync(userId, title, body ?? string.Empty, link ?? "/", ct);
    }

    public async Task<List<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, int limit, CancellationToken ct = default)
    {
        var query = _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.Link, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var entity = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct)
            ?? throw new NotFoundException(nameof(Notification), notificationId);
        if (!entity.IsRead)
        {
            entity.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);
        if (unread.Count == 0) return;
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}
```

**Lưu ý:** `NotificationService` constructor có thêm `IDateTime` (đã có `IDateTime` trong Infrastructure DI) và `IPushSender`. Verify rằng `IDateTime` đã registered (có — `services.AddSingleton<IDateTime, SystemDateTime>()`).

---

### Task 9 — PushSubscriptionConfiguration (Infrastructure)

**Tạo file** `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/PushSubscriptionConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderMgmt.Domain.Notifications;

namespace OrderMgmt.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> b)
    {
        b.ToTable("push_subscriptions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Endpoint).IsRequired().HasMaxLength(2048);
        b.Property(x => x.P256DH).IsRequired().HasMaxLength(512);
        b.Property(x => x.Auth).IsRequired().HasMaxLength(256);
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.UpdatedAt).HasColumnType("timestamptz");

        b.HasIndex(x => x.Endpoint).IsUnique()
            .HasDatabaseName("ix_push_subscriptions_endpoint");
        b.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_push_subscriptions_user_id");
    }
}
```

---

### Task 10 — Cập nhật AppDbContext (Infrastructure)

**Sửa** `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` — thêm sau `DbSet<Notification>`:

```csharp
public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
```

---

### Task 11 — Cập nhật DependencyInjection (Infrastructure)

**Sửa** `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` — thêm VapidOptions và PushSenderService:

```csharp
// Thêm using
using OrderMgmt.Application.Notifications.Interfaces;
using OrderMgmt.Infrastructure.Notifications;

// Thêm vào AddInfrastructure, sau services.AddScoped<IAppDbContext>:
services.Configure<VapidOptions>(configuration.GetSection(VapidOptions.SectionName));
services.AddScoped<IPushSender, PushSenderService>();
```

---

### Task 12 — Cập nhật appsettings.json (WebApi)

**Sửa** `backend/src/OrderMgmt.WebApi/appsettings.json` — thêm Vapid section với placeholder:

```json
"Vapid": {
  "PublicKey": "",
  "PrivateKey": "",
  "Subject": "mailto:admin@example.com"
}
```

Không commit keys thực. Keys thực được set qua `user-secrets` (dev) hoặc env var (prod).

Để generate VAPID keys:
```bash
# Cài WebPush CLI hoặc dùng dotnet script:
dotnet add package WebPush
# Sau đó trong một script tạm:
var keys = VapidHelper.GenerateVapidKeys();
Console.WriteLine($"Public: {keys.PublicKey}");
Console.WriteLine($"Private: {keys.PrivateKey}");
```

---

### Task 13 — Chạy EF Migration

```bash
cd backend
dotnet ef migrations add AddPushSubscriptions \
  --project src/OrderMgmt.Infrastructure \
  --startup-project src/OrderMgmt.WebApi \
  --output-dir Persistence/Migrations
```

Expected: migration file được tạo trong `Infrastructure/Persistence/Migrations/`.

Verify migration content: phải có `CreateTable("push_subscriptions", ...)` với đúng columns và indexes.

---

### Task 14 — Cập nhật WebAppFactory (Tests)

**Sửa** `backend/tests/OrderMgmt.IntegrationTests/Fixtures/WebAppFactory.cs` — thêm Vapid keys vào `AddInMemoryCollection`:

```csharp
["Vapid:PublicKey"] = "",
["Vapid:PrivateKey"] = "",
["Vapid:Subject"] = "mailto:test@test.com",
```

(Rỗng là đủ — `PushSenderService` skip khi PrivateKey rỗng.)

---

### Task 15 — PushSubscriptionController (WebApi)

**Tạo file** `backend/src/OrderMgmt.WebApi/Controllers/PushSubscriptionController.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Domain.Notifications;

namespace OrderMgmt.WebApi.Controllers;

[Authorize]
[Route("api/push")]
public class PushSubscriptionController : ApiControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PushSubscriptionController(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private Guid CurrentUserId =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated.");

    [HttpPost("subscribe")]
    public async Task<ActionResult<ApiResponse>> Subscribe(
        [FromBody] SubscribeRequest request,
        CancellationToken ct)
    {
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);

        if (existing is not null)
        {
            // Nếu endpoint đã thuộc về user khác (shared device), xóa rồi tạo mới
            if (existing.UserId != CurrentUserId)
                _db.PushSubscriptions.Remove(existing);
            else
            {
                existing.P256DH = request.P256DH;
                existing.Auth = request.Auth;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
                return Success();
            }
        }

        _db.PushSubscriptions.Add(new PushSubscription
        {
            UserId = CurrentUserId,
            Endpoint = request.Endpoint,
            P256DH = request.P256DH,
            Auth = request.Auth,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return Success();
    }

    [HttpDelete("subscribe")]
    public async Task<ActionResult<ApiResponse>> Unsubscribe(
        [FromBody] UnsubscribeRequest request,
        CancellationToken ct)
    {
        var sub = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint && s.UserId == CurrentUserId, ct);

        if (sub is not null)
        {
            _db.PushSubscriptions.Remove(sub);
            await _db.SaveChangesAsync(ct);
        }

        return Success();
    }

    public record SubscribeRequest(
        [Required, MaxLength(2048)] string Endpoint,
        [Required, MaxLength(512)] string P256DH,
        [Required, MaxLength(256)] string Auth);

    public record UnsubscribeRequest([Required, MaxLength(2048)] string Endpoint);
}
```

---

### Task 16 — Tích hợp vào QuotationService.TransitionAsync

**Sửa** `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`:

1. **Thêm** `INotificationService` vào constructor:

```csharp
private readonly INotificationService _notifications;

public QuotationService(
    IAppDbContext db,
    IDateTime clock,
    ICurrentUser currentUser,
    IQuotationExcelRenderer excelRenderer,
    IQuotationSpreadsheetPdfConverter pdfConverter,
    IOptionsMonitor<FeatureOptions> features,
    IQuotationExportPathResolver templatePathResolver,
    IHandoverExcelRenderer handoverRenderer,
    INotificationService notifications)       // thêm mới
{
    // ... existing assignments
    _notifications = notifications;
}
```

2. **Thêm using:** `using OrderMgmt.Application.Notifications.Interfaces;`

3. **Sau `await _db.SaveChangesAsync(ct)` trong `TransitionAsync`**, thêm notification dispatch:

```csharp
await _db.SaveChangesAsync(ct);
await DispatchQuotationNotificationAsync(quotation, next, ct);
return await GetAsync(quotation.Id, ct);
```

4. **Thêm private method** `DispatchQuotationNotificationAsync`:

```csharp
private static readonly HashSet<QuotationStatus> NotifiableStatuses = new()
{
    QuotationStatus.Confirmed,
    QuotationStatus.AccountingConfirmed,
    QuotationStatus.Cancelled,
};

private async Task DispatchQuotationNotificationAsync(
    Quotation quotation, QuotationStatus newStatus, CancellationToken ct)
{
    if (!NotifiableStatuses.Contains(newStatus)) return;

    var (title, body) = newStatus switch
    {
        QuotationStatus.Confirmed          => ("Báo giá đã xác nhận",   $"Báo giá {quotation.Code} đã được xác nhận."),
        QuotationStatus.AccountingConfirmed => ("Kế toán đã duyệt",     $"Báo giá {quotation.Code} đã được kế toán xác nhận."),
        QuotationStatus.Cancelled          => ("Báo giá bị hủy",        $"Báo giá {quotation.Code} đã bị hủy."),
        _                                  => (string.Empty, string.Empty),
    };

    var link = $"/quotations/{quotation.Id}";
    const string type = "quotation_status";

    // Collect recipients: owner + all Admin-role users
    var recipients = await _db.UserRoles
        .Where(ur => ur.Role!.Name == "Admin" && !ur.User!.IsDeleted)
        .Select(ur => ur.UserId)
        .ToListAsync(ct);

    if (!recipients.Contains(quotation.OwnerUserId))
        recipients.Add(quotation.OwnerUserId);

    foreach (var uid in recipients.Distinct())
        await _notifications.SendAsync(uid, type, title, body, link, ct);
}
```

**Lưu ý:** `UserRole` navigation `Role` cần include hoặc explicit join. Kiểm tra EF navigation properties trong `UserConfiguration.cs`.

---

### Task 17 — Chạy tests để verify PASS

```bash
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "PushSubscription" --logger "console;verbosity=normal"
```

Expected: 4 tests PASS.

5. **Commit sau khi all tests pass:**

```bash
git add backend/src/ backend/tests/
git commit -m "feat(pwa): push notification backend — entity, service, controller, quotation integration"
```

## Verification

```bash
# Run push subscription integration tests
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "PushSubscription" --logger "console;verbosity=normal"

# Run all integration tests (regression check)
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --logger "console;verbosity=normal"

# Build check
cd backend && dotnet build src/OrderMgmt.WebApi
```

## Exit Criteria

- [ ] `PushSubscriptionTests` — 4 tests PASS
- [ ] Toàn bộ integration test suite PASS (không có regression)
- [ ] `dotnet build` không có error/warning mới
- [ ] Migration file được tạo với đúng table + indexes
