# Phase 01 — Domain + Data

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm `ZaloGroupId` vào `Customer` entity, tạo singleton entity `ZaloOaToken`, chạy migration, và cập nhật `CustomerDto`/`CustomerService` để lưu/trả về `ZaloGroupId`.

## Files

- `backend/src/OrderMgmt.Domain/Entities/Catalog/Customer.cs`
- `backend/src/OrderMgmt.Domain/Integrations/ZaloOaToken.cs` ← new file
- `backend/src/OrderMgmt.Domain/Common/DomainException.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/CatalogConfiguration.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/ZaloOaTokenConfiguration.cs` ← new file
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs`
- `backend/src/OrderMgmt.Application/Catalog/Customers/Models/CustomerDto.cs`
- `backend/src/OrderMgmt.Application/Catalog/Customers/Services/CustomerService.cs`
- Migration files (generated)

---

## Tasks

### Task 1 — Integration test: customer update persists ZaloGroupId

1. **Write failing test** in `backend/tests/OrderMgmt.IntegrationTests/CustomerCrudTests.cs`:
   ```csharp
   [Fact]
   public async Task Update_sets_ZaloGroupId()
   {
       var create = await _client.PostAsJsonAsync("/api/customers",
           new CreateCustomerRequest { Name = "Zalo Test" });
       var created = (await create.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>(TestJson.Options))!.Data!;

       var update = await _client.PutAsJsonAsync($"/api/customers/{created.Id}",
           new UpdateCustomerRequest
           {
               Name = created.Name,
               Group = created.Group,
               Status = created.Status,
               ZaloGroupId = "1234567890",
           });
       update.StatusCode.Should().Be(HttpStatusCode.OK);
       var updated = (await update.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>(TestJson.Options))!.Data!;
       updated.ZaloGroupId.Should().Be("1234567890");
   }
   ```
2. **Run test** — Expected: FAIL with compilation error (no `ZaloGroupId` property)
   ```
   cd backend && dotnet test tests/OrderMgmt.IntegrationTests -k "Update_sets_ZaloGroupId"
   ```

3. **Implement** — edit `Customer.cs`, add after the `Note` property:
   ```csharp
   public string? ZaloGroupId { get; set; }
   ```

4. **Implement** — edit `CatalogConfiguration.cs`, add inside `CustomerConfiguration.Configure` after the `Note` property config:
   ```csharp
   b.Property(x => x.ZaloGroupId).HasMaxLength(50);
   ```

5. **Implement** — edit `CustomerDto.cs`, add `ZaloGroupId` to:
   - `CustomerDto`: `public string? ZaloGroupId { get; set; }`
   - `UpdateCustomerRequest`: `public string? ZaloGroupId { get; set; }`
   - `CreateCustomerRequest`: `public string? ZaloGroupId { get; set; }`

6. **Implement** — edit `CustomerService.cs`:
   - In `CreateAsync`, inside the `new Customer { ... }` initializer, add: `ZaloGroupId = request.ZaloGroupId?.Trim(),`
   - In `UpdateAsync`, after `customer.Note = request.Note;` add: `customer.ZaloGroupId = request.ZaloGroupId?.Trim();`

7. **Mapster note** — `CustomerDto` now has `ZaloGroupId`. The existing `config.NewConfig<Customer, CustomerDto>()` in `CustomerMappings.cs` auto-maps by convention (property name match). No change needed.

8. **Run test** — Expected: FAIL with `SqlException: column "zalo_group_id" does not exist` (migration not yet run)
   ```
   cd backend && dotnet test tests/OrderMgmt.IntegrationTests -k "Update_sets_ZaloGroupId"
   ```

9. **Generate migration** — from repo root:
   ```
   cd backend && dotnet ef migrations add AddZaloGroupIdToCustomers \
     --project src/OrderMgmt.Infrastructure \
     --startup-project src/OrderMgmt.WebApi \
     --output-dir Persistence/Migrations
   ```
   Verify `Persistence/Migrations/TIMESTAMP_AddZaloGroupIdToCustomers.cs` was created with:
   ```csharp
   migrationBuilder.AddColumn<string>(
       name: "zalo_group_id",
       table: "customers",
       type: "character varying(50)",
       maxLength: 50,
       nullable: true);
   ```

10. **Run test** — Expected: PASS
    ```
    cd backend && dotnet test tests/OrderMgmt.IntegrationTests -k "Update_sets_ZaloGroupId"
    ```

11. **Commit**:
    ```
    git commit -m "feat: add ZaloGroupId to Customer entity"
    ```

---

### Task 2 — ZaloOaToken singleton entity + migration

1. **Write failing test** in `backend/tests/OrderMgmt.IntegrationTests/Settings/ZaloTokenSettingsTests.cs` (new file):
   ```csharp
   using System.Net;
   using System.Net.Http.Headers;
   using System.Net.Http.Json;
   using FluentAssertions;
   using OrderMgmt.Application.Common.Models;
   using OrderMgmt.Application.Identity.Models;
   using OrderMgmt.IntegrationTests.Fixtures;
   using Xunit;

   namespace OrderMgmt.IntegrationTests.Settings;

   [Collection(nameof(PostgresCollection))]
   public class ZaloTokenSettingsTests : IAsyncLifetime
   {
       private readonly PostgresFixture _pg;
       private WebAppFactory _factory = default!;
       private HttpClient _client = default!;

       public ZaloTokenSettingsTests(PostgresFixture pg) => _pg = pg;

       public async Task InitializeAsync()
       {
           _factory = new WebAppFactory(_pg.ConnectionString);
           await ((IAsyncLifetime)_factory).InitializeAsync();
           _client = _factory.CreateClient();
           var login = await _client.PostAsJsonAsync("/api/auth/login",
               new LoginRequest { Username = "admin", Password = "Admin@123" });
           var body = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(TestJson.Options);
           _client.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
       }

       public async Task DisposeAsync()
       {
           _client.Dispose();
           await ((IAsyncLifetime)_factory).DisposeAsync();
       }

       [Fact]
       public async Task PutZaloToken_Admin_Returns200()
       {
           var response = await _client.PutAsJsonAsync("/api/settings/zalo-token", new
           {
               accessToken = "test_access_token",
               refreshToken = "test_refresh_token",
               accessTokenExpiresAt = DateTime.UtcNow.AddDays(90).ToString("O"),
               refreshTokenExpiresAt = DateTime.UtcNow.AddDays(90).ToString("O"),
           });
           response.StatusCode.Should().Be(HttpStatusCode.OK);
       }
   }
   ```
2. **Run test** — Expected: FAIL with compilation error (endpoint not yet created)
   ```
   cd backend && dotnet test tests/OrderMgmt.IntegrationTests -k "PutZaloToken_Admin_Returns200"
   ```

3. **Create** `backend/src/OrderMgmt.Domain/Integrations/ZaloOaToken.cs`:
   ```csharp
   namespace OrderMgmt.Domain.Integrations;

   public class ZaloOaToken
   {
       public int Id { get; set; }
       public string AccessToken { get; set; } = string.Empty;
       public string RefreshToken { get; set; } = string.Empty;
       public DateTime AccessTokenExpiresAt { get; set; }
       public DateTime RefreshTokenExpiresAt { get; set; }
       public DateTime LastRefreshedAt { get; set; }
   }
   ```

4. **Create** `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/ZaloOaTokenConfiguration.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using OrderMgmt.Domain.Integrations;

   namespace OrderMgmt.Infrastructure.Persistence.Configurations;

   public class ZaloOaTokenConfiguration : IEntityTypeConfiguration<ZaloOaToken>
   {
       public void Configure(EntityTypeBuilder<ZaloOaToken> b)
       {
           b.ToTable("zalo_oa_tokens");
           b.HasKey(x => x.Id);
           b.Property(x => x.Id).ValueGeneratedNever();
           b.Property(x => x.AccessToken).HasMaxLength(2000).IsRequired();
           b.Property(x => x.RefreshToken).HasMaxLength(2000).IsRequired();

           b.HasData(new ZaloOaToken
           {
               Id = 1,
               AccessToken = string.Empty,
               RefreshToken = string.Empty,
               AccessTokenExpiresAt = DateTime.UnixEpoch,
               RefreshTokenExpiresAt = DateTime.UnixEpoch,
               LastRefreshedAt = DateTime.UnixEpoch,
           });
       }
   }
   ```

5. **Edit** `IAppDbContext.cs` — add after `DbSet<Notification> Notifications { get; }`:
   ```csharp
   DbSet<ZaloOaToken> ZaloOaTokens { get; }
   ```
   Add import: `using OrderMgmt.Domain.Integrations;`

6. **Edit** `AppDbContext.cs` — add after `public DbSet<Notification> Notifications => Set<Notification>();`:
   ```csharp
   public DbSet<ZaloOaToken> ZaloOaTokens => Set<ZaloOaToken>();
   ```
   Add import: `using OrderMgmt.Domain.Integrations;`

7. **Add** `ExternalServiceException` to `DomainException.cs` — append at the end of the file:
   ```csharp
   public sealed class ExternalServiceException : Exception
   {
       public string ServiceName { get; }
       public ExternalServiceException(string serviceName, string message) : base(message)
       {
           ServiceName = serviceName;
       }
   }
   ```

8. **Edit** `GlobalExceptionMiddleware.cs` — add case before the `DomainException` catch-all in the switch expression:
   ```csharp
   ExternalServiceException ese => (StatusCodes.Status502BadGateway, new ApiError
   {
       Code = "EXTERNAL_SERVICE_ERROR",
       Message = ese.Message,
   }),
   ```
   Add import: `using OrderMgmt.Domain.Common;` (already there — no change needed)

9. **Generate migration**:
   ```
   cd backend && dotnet ef migrations add AddZaloOaTokens \
     --project src/OrderMgmt.Infrastructure \
     --startup-project src/OrderMgmt.WebApi \
     --output-dir Persistence/Migrations
   ```
   Verify migration creates table `zalo_oa_tokens` with seed row Id=1.

10. **Commit**:
    ```
    git commit -m "feat: add ZaloOaToken singleton entity and ExternalServiceException"
    ```

---

## Verification

```bash
cd backend
dotnet build OrderMgmt.sln
dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "Category=CustomerCrud|ZaloTokenSettings" 2>&1 | tail -20
```

## Exit Criteria

- `Customer.ZaloGroupId` property exists and is persisted through create/update
- `ZaloOaToken` table exists in DB with seeded row Id=1
- `CustomerDto.ZaloGroupId` is returned from GET/PUT customer endpoints
- `ExternalServiceException` compiles and is handled by middleware
- All existing customer tests still pass
