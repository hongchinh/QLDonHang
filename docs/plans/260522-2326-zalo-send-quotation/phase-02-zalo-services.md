# Phase 02 — Zalo Services

**Status:** [ ] pending
**Complexity:** M

## Objective

Tạo `IZaloTokenManager`, `IZaloOaService` interfaces trong Application layer, và `ZaloTokenManager`, `ZaloOaService` implementations trong Infrastructure layer. Đăng ký DI và thêm config template vào `appsettings.json`.

## Files

- `backend/src/OrderMgmt.Application/Integrations/Zalo/IZaloTokenManager.cs` ← new
- `backend/src/OrderMgmt.Application/Integrations/Zalo/IZaloOaService.cs` ← new
- `backend/src/OrderMgmt.Infrastructure/Zalo/ZaloOaOptions.cs` ← new
- `backend/src/OrderMgmt.Infrastructure/Zalo/ZaloTokenManager.cs` ← new
- `backend/src/OrderMgmt.Infrastructure/Zalo/ZaloOaService.cs` ← new
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs`
- `backend/src/OrderMgmt.WebApi/appsettings.json`
- `backend/src/OrderMgmt.WebApi/appsettings.Development.json`

---

## Tasks

### Task 1 — Application interfaces

No tests for pure interfaces. Create both files:

1. **Create** `backend/src/OrderMgmt.Application/Integrations/Zalo/IZaloTokenManager.cs`:
   ```csharp
   namespace OrderMgmt.Application.Integrations.Zalo;

   public interface IZaloTokenManager
   {
       /// <summary>
       /// Returns a valid access token. Auto-refreshes if expiry < 24 h.
       /// Throws DomainException("ZALO_TOKEN_EXPIRED") if both tokens are expired or not configured.
       /// </summary>
       Task<string> GetValidAccessTokenAsync(CancellationToken ct = default);

       Task UpdateTokensAsync(
           string accessToken,
           string refreshToken,
           DateTime accessTokenExpiresAt,
           DateTime refreshTokenExpiresAt,
           CancellationToken ct = default);
   }
   ```

2. **Create** `backend/src/OrderMgmt.Application/Integrations/Zalo/IZaloOaService.cs`:
   ```csharp
   namespace OrderMgmt.Application.Integrations.Zalo;

   public interface IZaloOaService
   {
       /// <summary>Uploads file bytes to Zalo. Returns the file token for use in SendFileToGroupAsync.</summary>
       Task<string> UploadFileAsync(byte[] fileData, string fileName, CancellationToken ct = default);

       /// <summary>Sends a text message to a Zalo group.</summary>
       Task SendTextToGroupAsync(string groupId, string text, CancellationToken ct = default);

       /// <summary>Sends a previously uploaded file (by token) to a Zalo group.</summary>
       Task SendFileToGroupAsync(string groupId, string fileToken, CancellationToken ct = default);
   }
   ```

3. **Commit**:
   ```
   git commit -m "feat: add IZaloTokenManager and IZaloOaService interfaces"
   ```

---

### Task 2 — ZaloOaOptions and ZaloTokenManager

1. **Create** `backend/src/OrderMgmt.Infrastructure/Zalo/ZaloOaOptions.cs`:
   ```csharp
   namespace OrderMgmt.Infrastructure.Zalo;

   public class ZaloOaOptions
   {
       public const string SectionName = "ZaloOA";

       public string AppId { get; set; } = string.Empty;
       public string AppSecret { get; set; } = string.Empty;
       public string UploadUrl { get; set; } = "https://upload.zaloapp.com/v2/oa/upload/file";
       public string MessageUrl { get; set; } = "https://openapi.zalo.me/v3.0/oa/group/sendmessage";
       public string TokenRefreshUrl { get; set; } = "https://oauth.zaloapp.com/v4/oa/access_token";
   }
   ```

2. **Create** `backend/src/OrderMgmt.Infrastructure/Zalo/ZaloTokenManager.cs`:
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.Extensions.Options;
   using OrderMgmt.Application.Common.Interfaces;
   using OrderMgmt.Application.Integrations.Zalo;
   using OrderMgmt.Domain.Common;
   using System.Net.Http.Json;

   namespace OrderMgmt.Infrastructure.Zalo;

   public class ZaloTokenManager : IZaloTokenManager
   {
       private readonly IAppDbContext _db;
       private readonly IHttpClientFactory _httpClientFactory;
       private readonly ZaloOaOptions _options;

       public ZaloTokenManager(
           IAppDbContext db,
           IHttpClientFactory httpClientFactory,
           IOptions<ZaloOaOptions> options)
       {
           _db = db;
           _httpClientFactory = httpClientFactory;
           _options = options.Value;
       }

       public async Task<string> GetValidAccessTokenAsync(CancellationToken ct = default)
       {
           var token = await _db.ZaloOaTokens.FirstOrDefaultAsync(t => t.Id == 1, ct);

           if (token is null || string.IsNullOrEmpty(token.AccessToken))
               throw new DomainException("ZALO_TOKEN_EXPIRED",
                   "Chưa cấu hình Zalo OA token. Vui lòng cập nhật qua Settings → Zalo Token.");

           // If token expires more than 24h from now, use it directly.
           if (token.AccessTokenExpiresAt > DateTime.UtcNow.AddHours(24))
               return token.AccessToken;

           // Access token expiring soon — attempt refresh.
           if (token.RefreshTokenExpiresAt <= DateTime.UtcNow)
               throw new DomainException("ZALO_TOKEN_EXPIRED",
                   "Zalo access token và refresh token đều đã hết hạn. Vui lòng lấy token mới từ Zalo Developer Portal.");

           return await RefreshAsync(token, ct);
       }

       public async Task UpdateTokensAsync(
           string accessToken,
           string refreshToken,
           DateTime accessTokenExpiresAt,
           DateTime refreshTokenExpiresAt,
           CancellationToken ct = default)
       {
           var token = await _db.ZaloOaTokens.FirstAsync(t => t.Id == 1, ct);
           token.AccessToken = accessToken;
           token.RefreshToken = refreshToken;
           token.AccessTokenExpiresAt = accessTokenExpiresAt;
           token.RefreshTokenExpiresAt = refreshTokenExpiresAt;
           token.LastRefreshedAt = DateTime.UtcNow;
           await _db.SaveChangesAsync(ct);
       }

       private async Task<string> RefreshAsync(Domain.Integrations.ZaloOaToken token, CancellationToken ct)
       {
           var client = _httpClientFactory.CreateClient();
           var form = new FormUrlEncodedContent(new[]
           {
               new KeyValuePair<string, string>("app_id", _options.AppId),
               new KeyValuePair<string, string>("grant_type", "refresh_token"),
               new KeyValuePair<string, string>("refresh_token", token.RefreshToken),
           });

           var response = await client.PostAsync(_options.TokenRefreshUrl, form, ct);
           if (!response.IsSuccessStatusCode)
               throw new ExternalServiceException("ZaloOA",
                   $"Zalo token refresh thất bại: HTTP {(int)response.StatusCode}");

           var result = await response.Content.ReadFromJsonAsync<ZaloRefreshResponse>(cancellationToken: ct)
               ?? throw new ExternalServiceException("ZaloOA", "Zalo token refresh trả về dữ liệu không hợp lệ.");

           if (!string.IsNullOrEmpty(result.Error))
               throw new ExternalServiceException("ZaloOA",
                   $"Zalo token refresh lỗi: {result.Message ?? result.Error}");

           var expiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
           token.AccessToken = result.AccessToken;
           token.RefreshToken = result.RefreshToken;
           token.AccessTokenExpiresAt = expiresAt;
           // Refresh token expiry: Zalo returns same expires_in for refresh token.
           // Keep existing RefreshTokenExpiresAt to avoid overwriting with potentially shorter value.
           token.LastRefreshedAt = DateTime.UtcNow;
           await _db.SaveChangesAsync(ct);

           return token.AccessToken;
       }

       private sealed class ZaloRefreshResponse
       {
           public string AccessToken { get; set; } = string.Empty;
           public string RefreshToken { get; set; } = string.Empty;
           public int ExpiresIn { get; set; }
           public string? Error { get; set; }
           public string? Message { get; set; }
       }
   }
   ```

3. **Commit**:
   ```
   git commit -m "feat: add ZaloOaOptions and ZaloTokenManager with proactive refresh"
   ```

---

### Task 3 — ZaloOaService

1. **Create** `backend/src/OrderMgmt.Infrastructure/Zalo/ZaloOaService.cs`:
   ```csharp
   using Microsoft.Extensions.Options;
   using OrderMgmt.Application.Integrations.Zalo;
   using OrderMgmt.Domain.Common;
   using System.Net.Http.Json;
   using System.Text;
   using System.Text.Json;

   namespace OrderMgmt.Infrastructure.Zalo;

   public class ZaloOaService : IZaloOaService
   {
       private readonly HttpClient _http;
       private readonly IZaloTokenManager _tokenManager;
       private readonly ZaloOaOptions _options;

       public ZaloOaService(
           HttpClient http,
           IZaloTokenManager tokenManager,
           IOptions<ZaloOaOptions> options)
       {
           _http = http;
           _tokenManager = tokenManager;
           _options = options.Value;
       }

       public async Task<string> UploadFileAsync(byte[] fileData, string fileName, CancellationToken ct = default)
       {
           var accessToken = await _tokenManager.GetValidAccessTokenAsync(ct);

           using var content = new MultipartFormDataContent();
           var fileContent = new ByteArrayContent(fileData);
           fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
               fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                   ? "application/pdf"
                   : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
           content.Add(fileContent, "file", fileName);

           using var request = new HttpRequestMessage(HttpMethod.Post, _options.UploadUrl)
           {
               Headers = { { "access_token", accessToken } },
               Content = content,
           };

           var response = await _http.SendAsync(request, ct);
           if (!response.IsSuccessStatusCode)
               throw new ExternalServiceException("ZaloOA",
                   $"Zalo file upload thất bại: HTTP {(int)response.StatusCode}");

           var result = await response.Content.ReadFromJsonAsync<ZaloUploadResponse>(cancellationToken: ct)
               ?? throw new ExternalServiceException("ZaloOA", "Zalo upload trả về dữ liệu không hợp lệ.");

           if (result.Error != 0)
               throw new ExternalServiceException("ZaloOA",
                   $"Zalo upload lỗi {result.Error}: {result.Message}");

           return result.Data?.Token
               ?? throw new ExternalServiceException("ZaloOA", "Zalo upload không trả về file token.");
       }

       public async Task SendTextToGroupAsync(string groupId, string text, CancellationToken ct = default)
       {
           var accessToken = await _tokenManager.GetValidAccessTokenAsync(ct);
           var payload = new
           {
               recipient = new { group_id = groupId },
               message = new { text }
           };
           await PostMessageAsync(accessToken, payload, ct);
       }

       public async Task SendFileToGroupAsync(string groupId, string fileToken, CancellationToken ct = default)
       {
           var accessToken = await _tokenManager.GetValidAccessTokenAsync(ct);
           var payload = new
           {
               recipient = new { group_id = groupId },
               message = new
               {
                   attachments = new[]
                   {
                       new { type = "file", payload = new { token = fileToken } }
                   }
               }
           };
           await PostMessageAsync(accessToken, payload, ct);
       }

       private async Task PostMessageAsync(string accessToken, object payload, CancellationToken ct)
       {
           var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
           {
               PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
           });
           using var request = new HttpRequestMessage(HttpMethod.Post, _options.MessageUrl)
           {
               Headers = { { "access_token", accessToken } },
               Content = new StringContent(json, Encoding.UTF8, "application/json"),
           };

           var response = await _http.SendAsync(request, ct);
           if (!response.IsSuccessStatusCode)
               throw new ExternalServiceException("ZaloOA",
                   $"Zalo message gửi thất bại: HTTP {(int)response.StatusCode}");

           var result = await response.Content.ReadFromJsonAsync<ZaloMessageResponse>(cancellationToken: ct)
               ?? throw new ExternalServiceException("ZaloOA", "Zalo API trả về dữ liệu không hợp lệ.");

           if (result.Error != 0)
               throw new ExternalServiceException("ZaloOA",
                   $"Zalo message lỗi {result.Error}: {result.Message}");
       }

       private sealed class ZaloUploadResponse
       {
           public int Error { get; set; }
           public string? Message { get; set; }
           public ZaloUploadData? Data { get; set; }
       }

       private sealed class ZaloUploadData
       {
           public string? Token { get; set; }
       }

       private sealed class ZaloMessageResponse
       {
           public int Error { get; set; }
           public string? Message { get; set; }
       }
   }
   ```

2. **Commit**:
   ```
   git commit -m "feat: add ZaloOaService for file upload and group messaging"
   ```

---

### Task 4 — Register services in DI + config

1. **Edit** `DependencyInjection.cs` — add after the `QuotationExport` block:
   ```csharp
   services.Configure<ZaloOaOptions>(configuration.GetSection(ZaloOaOptions.SectionName));
   services.AddHttpClient<IZaloOaService, ZaloOaService>();
   services.AddScoped<IZaloTokenManager, ZaloTokenManager>();
   ```
   Add usings:
   ```csharp
   using OrderMgmt.Application.Integrations.Zalo;
   using OrderMgmt.Infrastructure.Zalo;
   ```

2. **Edit** `appsettings.json` — add section after `QuotationExport`:
   ```json
   "ZaloOA": {
     "AppId": "",
     "AppSecret": "",
     "UploadUrl": "https://upload.zaloapp.com/v2/oa/upload/file",
     "MessageUrl": "https://openapi.zalo.me/v3.0/oa/group/sendmessage",
     "TokenRefreshUrl": "https://oauth.zaloapp.com/v4/oa/access_token"
   }
   ```

3. **Edit** `appsettings.Development.json` — add same section with empty `AppId`/`AppSecret` (tokens are stored in DB via settings endpoint):
   ```json
   "ZaloOA": {
     "AppId": "",
     "AppSecret": "",
     "UploadUrl": "https://upload.zaloapp.com/v2/oa/upload/file",
     "MessageUrl": "https://openapi.zalo.me/v3.0/oa/group/sendmessage",
     "TokenRefreshUrl": "https://oauth.zaloapp.com/v4/oa/access_token"
   }
   ```

4. **Edit** `WebAppFactory.cs` — add to the in-memory config dictionary:
   ```csharp
   ["ZaloOA:AppId"] = "test-app-id",
   ["ZaloOA:AppSecret"] = "test-app-secret",
   ```

5. **Build check**:
   ```
   cd backend && dotnet build OrderMgmt.sln
   ```
   Expected: Build success, 0 errors.

6. **Commit**:
   ```
   git commit -m "feat: register ZaloOaService and ZaloTokenManager in DI"
   ```

---

## Verification

```bash
cd backend && dotnet build OrderMgmt.sln 2>&1 | tail -5
```
Expected: `Build succeeded.`

## Exit Criteria

- `IZaloTokenManager` and `IZaloOaService` compile without errors
- `ZaloTokenManager` and `ZaloOaService` compile without errors
- `ZaloOaOptions` section registered in DI
- `IZaloOaService` registered as typed HttpClient
- `IZaloTokenManager` registered as Scoped
- `dotnet build` passes clean
