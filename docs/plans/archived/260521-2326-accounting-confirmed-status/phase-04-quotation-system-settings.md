# Phase 04 — QuotationSystemSettings Service & Controller

**Status:** [ ] pending
**Complexity:** M

## Objective

Xây dựng service + controller để admin đọc/ghi `QuotationSystemSettings` (config `RevenueReportingDateField`). Gated bởi role ADMIN.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationSystemSettingsDto.cs` ← file mới
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationSystemSettingsService.cs` ← file mới
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationSystemSettingsService.cs` ← file mới
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` (hoặc file DI tương đương)
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationSettingsController.cs` ← file mới

## Tasks

### DTO

1. **Tạo `QuotationSystemSettingsDto.cs`**:
   ```csharp
   namespace OrderMgmt.Application.Sales.Quotations.Models;

   public class QuotationSystemSettingsDto
   {
       // "QuotationDate" | "ConfirmedAt" | "AccountingConfirmedAt"
       public string RevenueReportingDateField { get; set; } = "QuotationDate";
       public DateTimeOffset UpdatedAt { get; set; }
       public Guid? UpdatedBy { get; set; }
   }

   public class UpdateQuotationSystemSettingsRequest
   {
       public string RevenueReportingDateField { get; set; } = "QuotationDate";
   }
   ```

### Interface

2. **Tạo `IQuotationSystemSettingsService.cs`**:
   ```csharp
   namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

   public interface IQuotationSystemSettingsService
   {
       Task<QuotationSystemSettingsDto> GetAsync(CancellationToken ct = default);
       Task<QuotationSystemSettingsDto> UpdateAsync(UpdateQuotationSystemSettingsRequest request, CancellationToken ct = default);
   }
   ```

### Service

3. **Tạo `QuotationSystemSettingsService.cs`**:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using OrderMgmt.Application.Common.Interfaces;
   using OrderMgmt.Application.Sales.Quotations.Interfaces;
   using OrderMgmt.Application.Sales.Quotations.Models;
   using OrderMgmt.Domain.Entities.Sales;

   namespace OrderMgmt.Application.Sales.Quotations.Services;

   public class QuotationSystemSettingsService : IQuotationSystemSettingsService
   {
       private static readonly string[] AllowedValues = ["QuotationDate", "ConfirmedAt", "AccountingConfirmedAt"];
       private readonly IAppDbContext _db;
       private readonly IDateTime _clock;
       private readonly ICurrentUser _currentUser;

       public QuotationSystemSettingsService(IAppDbContext db, IDateTime clock, ICurrentUser currentUser)
       {
           _db = db;
           _clock = clock;
           _currentUser = currentUser;
       }

       public async Task<QuotationSystemSettingsDto> GetAsync(CancellationToken ct = default)
       {
           var settings = await _db.QuotationSystemSettings.FirstAsync(s => s.Id == 1, ct);
           return new QuotationSystemSettingsDto
           {
               RevenueReportingDateField = settings.RevenueReportingDateField,
               UpdatedAt = settings.UpdatedAt,
               UpdatedBy = settings.UpdatedBy,
           };
       }

       public async Task<QuotationSystemSettingsDto> UpdateAsync(
           UpdateQuotationSystemSettingsRequest request, CancellationToken ct = default)
       {
           if (!AllowedValues.Contains(request.RevenueReportingDateField))
               throw new DomainException("VALIDATION",
                   $"Giá trị '{request.RevenueReportingDateField}' không hợp lệ. " +
                   $"Chấp nhận: {string.Join(", ", AllowedValues)}");

           var settings = await _db.QuotationSystemSettings.FirstAsync(s => s.Id == 1, ct);
           settings.RevenueReportingDateField = request.RevenueReportingDateField;
           settings.UpdatedAt = _clock.UtcNow;
           settings.UpdatedBy = _currentUser.UserId;
           await _db.SaveChangesAsync(ct);
           return await GetAsync(ct);
       }
   }
   ```

### DI Registration

4. **Trong DI/`DependencyInjection.cs` của Infrastructure hoặc Application** — đăng ký:
   ```csharp
   services.AddScoped<IQuotationSystemSettingsService, QuotationSystemSettingsService>();
   ```
   Tìm file DI registration hiện tại của project (thường là `DependencyInjection.cs` hoặc `ServiceCollectionExtensions.cs`). Đặt cạnh các service đăng ký khác của Quotations.

### Controller

5. **Tạo `QuotationSettingsController.cs`** — toàn bộ codebase dùng `[HasPermission]` (không dùng `[Authorize(Roles=...)]`). Dùng `Permissions.System.ManageSettings` được định nghĩa ở Phase 01:
   ```csharp
   using Microsoft.AspNetCore.Mvc;
   using OrderMgmt.Application.Common.Models;
   using OrderMgmt.Application.Sales.Quotations.Interfaces;
   using OrderMgmt.Application.Sales.Quotations.Models;
   using OrderMgmt.Domain.Constants;
   using OrderMgmt.WebApi.Filters;

   namespace OrderMgmt.WebApi.Controllers;

   [Route("api/settings/quotation")]
   public class QuotationSettingsController : ApiControllerBase
   {
       private readonly IQuotationSystemSettingsService _settings;

       public QuotationSettingsController(IQuotationSystemSettingsService settings)
           => _settings = settings;

       [HttpGet]
       [HasPermission(Permissions.System.ManageSettings)]
       public async Task<ActionResult<ApiResponse<QuotationSystemSettingsDto>>> Get(CancellationToken ct)
           => Success(await _settings.GetAsync(ct));

       [HttpPut]
       [HasPermission(Permissions.System.ManageSettings)]
       public async Task<ActionResult<ApiResponse<QuotationSystemSettingsDto>>> Update(
           [FromBody] UpdateQuotationSystemSettingsRequest request, CancellationToken ct)
           => Success(await _settings.UpdateAsync(request, ct));
   }
   ```
   
   Lưu ý: Kiểm tra namespace thực tế của `HasPermissionAttribute` trong project (xem các controller hiện tại như `AdminUsersController.cs` để copy using đúng).

## Verification

```bash
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj -nologo --verbosity minimal
```

Sau restart WebApi: `GET /api/settings/quotation` với ADMIN token trả về `{"revenueReportingDateField":"QuotationDate",...}`.

## Exit Criteria

- Application + WebApi build thành công
- `GET /api/settings/quotation` trả về 200 với user có `system.manage_settings`, 403 với user không có permission, 401 với anonymous
- `PUT /api/settings/quotation` cập nhật và persist đúng
- Invalid value bị reject 400/conflict
