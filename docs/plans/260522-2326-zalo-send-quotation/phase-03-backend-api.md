# Phase 03 — Backend API

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm `SendToZaloAsync` vào `QuotationService`, endpoint `POST /quotations/{id}/send-zalo`, và endpoint `PUT /api/settings/zalo-token`. Viết integration tests với fake `IZaloOaService`.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/SettingsController.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationSendZaloTests.cs` ← new
- `backend/tests/OrderMgmt.IntegrationTests/Settings/ZaloTokenSettingsTests.cs` (extend)

---

## Tasks

### Task 1 — Integration tests (failing first)

1. **Create** `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationSendZaloTests.cs`:
   ```csharp
   using System.Net;
   using System.Net.Http.Json;
   using FluentAssertions;
   using Microsoft.AspNetCore.Hosting;
   using Microsoft.Extensions.DependencyInjection;
   using OrderMgmt.Application.Catalog.Customers.Models;
   using OrderMgmt.Application.Common.Models;
   using OrderMgmt.Application.Integrations.Zalo;
   using OrderMgmt.Application.Sales.Quotations.Models;
   using OrderMgmt.IntegrationTests.Fixtures;
   using Xunit;

   namespace OrderMgmt.IntegrationTests.Quotations;

   [Collection(nameof(PostgresCollection))]
   public class QuotationSendZaloTests : QuotationTestBase
   {
       public QuotationSendZaloTests(PostgresFixture pg) : base(pg) { }

       // QuotationTestBase.InitializeAsync is virtual — override to swap in fake Zalo service.
       public override async Task InitializeAsync()
       {
           _factory = new WebAppFactoryWithFakeZaloOaService(_pg.ConnectionString);
           await ((IAsyncLifetime)_factory).InitializeAsync();
           _client = _factory.CreateClient();
           await AuthenticateAsync("admin", "Admin@123");
           await SeedReferenceDataAsync();
       }

       [Fact]
       public async Task SendZalo_Excel_Returns200_WhenGroupIdSet()
       {
           // Arrange: create customer with ZaloGroupId
           var custResp = await _client.PostAsJsonAsync("/api/customers",
               new CreateCustomerRequest
               {
                   Name = "Zalo Customer",
                   Group = OrderMgmt.Domain.Enums.CustomerGroup.Company,
                   ZaloGroupId = "9999999999",
               });
           custResp.EnsureSuccessStatusCode();
           var cust = (await custResp.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>(TestJson.Options))!.Data!;

           // Create quotation for that customer
           var req = BuildRequest();
           req.CustomerId = cust.Id;
           var create = await _client.PostAsJsonAsync("/api/quotations", req);
           var created = (await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options))!.Data!;

           // Act
           var response = await _client.PostAsJsonAsync(
               $"/api/quotations/{created.Id}/send-zalo",
               new { fileType = "excel" });

           // Assert
           response.StatusCode.Should().Be(HttpStatusCode.OK);
       }

       [Fact]
       public async Task SendZalo_Returns400_WhenGroupIdMissing()
       {
           var req = BuildRequest(); // default customer has no ZaloGroupId
           var create = await _client.PostAsJsonAsync("/api/quotations", req);
           var created = (await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options))!.Data!;

           var response = await _client.PostAsJsonAsync(
               $"/api/quotations/{created.Id}/send-zalo",
               new { fileType = "excel" });

           response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
           var body = await response.Content.ReadFromJsonAsync<ApiResponse>(TestJson.Options);
           body!.Error!.Code.Should().Be("ZALO_GROUP_ID_MISSING");
       }
   }

   file sealed class WebAppFactoryWithFakeZaloOaService : WebAppFactory
   {
       public WebAppFactoryWithFakeZaloOaService(string connectionString) : base(connectionString) { }

       protected override void ConfigureWebHost(IWebHostBuilder builder)
       {
           base.ConfigureWebHost(builder);
           builder.ConfigureServices(services =>
           {
               var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IZaloOaService));
               if (descriptor is not null) services.Remove(descriptor);
               services.AddScoped<IZaloOaService, FakeZaloOaService>();
           });
       }
   }

   file sealed class FakeZaloOaService : IZaloOaService
   {
       public Task<string> UploadFileAsync(byte[] fileData, string fileName, CancellationToken ct = default)
           => Task.FromResult("fake-file-token");

       public Task SendTextToGroupAsync(string groupId, string text, CancellationToken ct = default)
           => Task.CompletedTask;

       public Task SendFileToGroupAsync(string groupId, string fileToken, CancellationToken ct = default)
           => Task.CompletedTask;
   }
   ```

2. **Run tests** — Expected: FAIL (compilation error — no `send-zalo` endpoint and no `SendToZaloAsync`)
   ```
   cd backend && dotnet test tests/OrderMgmt.IntegrationTests -k "SendZalo"
   ```

---

### Task 2 — SendQuotationToZaloRequest DTO

1. **Edit** `QuotationDto.cs` — add at the end of the file:
   ```csharp
   public record SendQuotationToZaloRequest(string FileType); // "excel" | "pdf"
   ```

---

### Task 3 — IQuotationService extension

1. **Edit** `IQuotationService.cs` — add after `RenderPdfAsync`:
   ```csharp
   Task SendToZaloAsync(Guid id, string fileType, CancellationToken ct = default);
   ```

---

### Task 4 — QuotationService.SendToZaloAsync implementation

1. **Edit** `QuotationService.cs`:

   Add `IZaloOaService` to constructor:
   ```csharp
   private readonly IZaloOaService _zaloOaService;

   public QuotationService(
       IAppDbContext db,
       IDateTime clock,
       ICurrentUser currentUser,
       IQuotationExcelRenderer excelRenderer,
       IQuotationSpreadsheetPdfConverter pdfConverter,
       IOptionsMonitor<FeatureOptions> features,
       IQuotationExportPathResolver templatePathResolver,
       IZaloOaService zaloOaService)   // ← add
   {
       // existing assignments...
       _zaloOaService = zaloOaService;
   }
   ```
   Add using: `using OrderMgmt.Application.Integrations.Zalo;`

   Add method implementation after `RenderPdfAsync`:
   ```csharp
   public async Task SendToZaloAsync(Guid id, string fileType, CancellationToken ct = default)
   {
       var quotation = await _db.Quotations
           .AsNoTracking()
           .Include(q => q.Customer)
           .FirstOrDefaultAsync(q => q.Id == id, ct)
           ?? throw new NotFoundException(nameof(Quotation), id);

       EnsureCanAccess(quotation);

       var groupId = quotation.Customer?.ZaloGroupId;
       if (string.IsNullOrWhiteSpace(groupId))
           throw new DomainException("ZALO_GROUP_ID_MISSING",
               "Khách hàng chưa có Zalo Group ID. Vui lòng cập nhật hồ sơ khách hàng trước.");

       // Generate file
       byte[] fileBytes;
       string fileName;
       if (fileType.Equals("pdf", StringComparison.OrdinalIgnoreCase))
       {
           (fileBytes, fileName) = await RenderPdfAsync(id, ct);
       }
       else
       {
           (fileBytes, fileName) = await RenderExcelAsync(id, ct);
       }

       // Upload to Zalo
       var fileToken = await _zaloOaService.UploadFileAsync(fileBytes, fileName, ct);

       // Send text message then file
       var textMessage = $"Báo giá {quotation.Code} ngày {quotation.QuotationDate:dd/MM/yyyy}";
       await _zaloOaService.SendTextToGroupAsync(groupId, textMessage, ct);
       await _zaloOaService.SendFileToGroupAsync(groupId, fileToken, ct);
   }
   ```

---

### Task 5 — POST /quotations/{id}/send-zalo endpoint

1. **Edit** `QuotationsController.cs`:

   Add validator field:
   ```csharp
   private readonly IValidator<SendQuotationToZaloRequest> _sendZaloValidator;
   ```

   Add to constructor parameter list and assignment:
   ```csharp
   IValidator<SendQuotationToZaloRequest> sendZaloValidator
   // ...
   _sendZaloValidator = sendZaloValidator;
   ```

   Add endpoint after `Clone`:
   ```csharp
   [HttpPost("{id:guid}/send-zalo")]
   [HasPermission(Permissions.Quotations.Print)]
   [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
   [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
   [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status502BadGateway)]
   public async Task<ActionResult<ApiResponse>> SendZalo(
       Guid id,
       [FromBody] SendQuotationToZaloRequest request,
       CancellationToken ct)
   {
       await _sendZaloValidator.ValidateAndThrowAsync(request, ct);
       await _quotations.SendToZaloAsync(id, request.FileType, ct);
       return Success();
   }
   ```

2. **Add validator** in `QuotationValidators.cs` — add at the end of the file:
   ```csharp
   public class SendQuotationToZaloRequestValidator : AbstractValidator<SendQuotationToZaloRequest>
   {
       private static readonly string[] AllowedTypes = { "excel", "pdf" };

       public SendQuotationToZaloRequestValidator()
       {
           RuleFor(x => x.FileType)
               .NotEmpty()
               .Must(t => AllowedTypes.Contains(t.ToLowerInvariant()))
               .WithMessage("fileType phải là 'excel' hoặc 'pdf'.");
       }
   }
   ```

---

### Task 6 — PUT /api/settings/zalo-token endpoint

1. **Edit** `SettingsController.cs`:

   Add `IZaloTokenManager` dependency and constructor injection:
   ```csharp
   private readonly IZaloTokenManager _zaloTokenManager;

   public SettingsController(IBrandingService branding, IZaloTokenManager zaloTokenManager)
   {
       _branding = branding;
       _zaloTokenManager = zaloTokenManager;
   }
   ```
   Add usings: `using OrderMgmt.Application.Integrations.Zalo;`

   Add DTO record in `SettingsController.cs` file scope (outside the class, or as a nested record):
   ```csharp
   public record UpdateZaloTokenRequest(
       string AccessToken,
       string RefreshToken,
       DateTime AccessTokenExpiresAt,
       DateTime RefreshTokenExpiresAt);
   ```

   Add endpoint:
   ```csharp
   [HttpPut("zalo-token")]
   [HasPermission(Permissions.System.ManageSettings)]
   public async Task<ActionResult<ApiResponse>> UpdateZaloToken(
       [FromBody] UpdateZaloTokenRequest request,
       CancellationToken ct)
   {
       await _zaloTokenManager.UpdateTokensAsync(
           request.AccessToken,
           request.RefreshToken,
           request.AccessTokenExpiresAt,
           request.RefreshTokenExpiresAt,
           ct);
       return Success();
   }
   ```
   Add using: `using OrderMgmt.Domain.Constants;`

---

### Task 7 — Run all tests

1. **Run** integration tests:
   ```
   cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
     --filter "QuotationSendZalo|ZaloTokenSettings"
   ```
   Expected: All PASS.

2. **Run** full suite:
   ```
   cd backend && dotnet test tests/OrderMgmt.IntegrationTests
   ```
   Expected: All existing tests still pass.

3. **Commit**:
   ```
   git commit -m "feat: add send-to-zalo endpoint and zalo-token settings endpoint"
   ```

---

## Verification

```bash
cd backend
dotnet build OrderMgmt.sln
dotnet test tests/OrderMgmt.IntegrationTests 2>&1 | tail -10
```

## Exit Criteria

- `POST /quotations/{id}/send-zalo` returns 200 when customer has `ZaloGroupId` (with fake service)
- `POST /quotations/{id}/send-zalo` returns 400 with code `ZALO_GROUP_ID_MISSING` when customer has none
- `PUT /api/settings/zalo-token` returns 200 for admin
- All existing integration tests still pass
