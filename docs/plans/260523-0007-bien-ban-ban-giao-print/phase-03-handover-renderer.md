# Phase 03 — Handover Excel Renderer

**Status:** [ ] pending
**Complexity:** M

## Objective

Tạo `IHandoverExcelRenderer` interface và `HandoverExcelRenderer` implementation để render biên bản bàn giao từ `QuotationDto`, hỗ trợ cả 2 variant (có tiền / không tiền). Đăng ký trong DI.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IHandoverExcelRenderer.cs` (mới)
- `backend/src/OrderMgmt.Infrastructure/Excel/HandoverExcelRenderer.cs` (mới)
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/HandoverExportTests.cs` (mới)

## Pre-task: Kiểm tra template layout

**Trước khi viết renderer**, mở 2 file template để xác định cell addresses:

1. Mở `backend/src/OrderMgmt.WebApi/templates/templete_bbbg.xlsx` trong Excel/LibreOffice Calc (giả định: có tiền)
2. Mở `backend/src/OrderMgmt.WebApi/templates/templete_bbbg_sl.xlsx` (giả định: không tiền)

Xác định và ghi lại:
- Ô chứa số báo giá / mã phiếu
- Ô chứa ngày bàn giao (`DeliveryDate`)
- Ô chứa tên khách hàng (`CustomerName`)
- Ô chứa địa chỉ giao hàng (`DeliveryAddress`)
- Row đầu tiên của danh sách hàng hóa (`FirstSampleRow`)
- Số sample rows (`SampleRowCount`)
- **Tổng số cột có nội dung** (`MaxColumn` — dùng cho `CopyRowStyle`)
- Cột: STT, tên hàng, DVT, số lượng, đơn giá (chỉ có tiền), thành tiền (chỉ có tiền)
- Ô tạm ứng (chỉ template có tiền)

Nếu 2 template có cùng layout: dùng 1 renderer với `bool withPrice` switch.
Nếu layout khác nhau: cần 2 bộ constants riêng nhưng có thể dùng chung code render rows.

## Tasks

### Task 3.1 — Tạo interface `IHandoverExcelRenderer`

1. **Tạo file** `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IHandoverExcelRenderer.cs`:

   ```csharp
   using OrderMgmt.Application.Sales.Quotations.Models;

   namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

   public interface IHandoverExcelRenderer
   {
       Task<byte[]> RenderAsync(
           QuotationDto quotation,
           string templatePath,
           bool withPrice,
           CancellationToken ct = default);
   }
   ```

2. **Build Application:**
   ```
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   ```
   Expected: 0 errors.

### Task 3.2 — Viết test trước khi implement (TDD)

1. **Tạo file** `backend/tests/OrderMgmt.IntegrationTests/Quotations/HandoverExportTests.cs`:

   ```csharp
   using System.Net;
   using System.Net.Http.Json;
   using FluentAssertions;
   using OrderMgmt.Application.Common.Models;
   using OrderMgmt.Application.Sales.Quotations.Models;
   using OrderMgmt.IntegrationTests.Fixtures;
   using Xunit;

   namespace OrderMgmt.IntegrationTests.Quotations;

   [Collection(nameof(PostgresCollection))]
   public class HandoverExportTests : QuotationTestBase
   {
       public HandoverExportTests(PostgresFixture pg) : base(pg) { }

       [Fact]
       public async Task HandoverWithPrice_Excel_returns_xlsx()
       {
           var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
           var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
           var id = created!.Data!.Id;

           var response = await _client.GetAsync($"/api/quotations/{id}/handover-with-price/excel");

           response.StatusCode.Should().Be(HttpStatusCode.OK);
           response.Content.Headers.ContentType!.MediaType.Should().Be(
               "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
           (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
       }

       [Fact]
       public async Task HandoverNoPrice_Excel_returns_xlsx()
       {
           var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
           var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
           var id = created!.Data!.Id;

           var response = await _client.GetAsync($"/api/quotations/{id}/handover-no-price/excel");

           response.StatusCode.Should().Be(HttpStatusCode.OK);
           response.Content.Headers.ContentType!.MediaType.Should().Be(
               "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
           (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
       }

       [Fact]
       public async Task HandoverWithPrice_Pdf_returns_pdf_via_fake_converter()
       {
           var factory = new WebAppFactoryWithFakeHandoverPdfConverter(_pg.ConnectionString);
           await ((IAsyncLifetime)factory).InitializeAsync();
           var client = factory.CreateClient();
           await AuthenticateClientAsync(client);

           var create = await client.PostAsJsonAsync("/api/quotations", BuildRequest());
           var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
           var id = created!.Data!.Id;

           var response = await client.GetAsync($"/api/quotations/{id}/handover-with-price/pdf");

           response.StatusCode.Should().Be(HttpStatusCode.OK);
           response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

           client.Dispose();
           await ((IAsyncLifetime)factory).DisposeAsync();
       }

       [Fact]
       public async Task HandoverNoPrice_Pdf_returns_pdf_via_fake_converter()
       {
           var factory = new WebAppFactoryWithFakeHandoverPdfConverter(_pg.ConnectionString);
           await ((IAsyncLifetime)factory).InitializeAsync();
           var client = factory.CreateClient();
           await AuthenticateClientAsync(client);

           var create = await client.PostAsJsonAsync("/api/quotations", BuildRequest());
           var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
           var id = created!.Data!.Id;

           var response = await client.GetAsync($"/api/quotations/{id}/handover-no-price/pdf");

           response.StatusCode.Should().Be(HttpStatusCode.OK);
           response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

           client.Dispose();
           await ((IAsyncLifetime)factory).DisposeAsync();
       }

       private static async Task AuthenticateClientAsync(HttpClient client)
       {
           var response = await client.PostAsJsonAsync("/api/auth/login",
               new LoginRequest { Username = "admin", Password = "Admin@123" });
           response.EnsureSuccessStatusCode();
           var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(TestJson.Options);
           client.DefaultRequestHeaders.Authorization =
               new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
       }
   }

   // Trong file riêng hoặc thêm vào file này
   file sealed class WebAppFactoryWithFakeHandoverPdfConverter : WebAppFactory
   {
       public WebAppFactoryWithFakeHandoverPdfConverter(string connectionString) : base(connectionString) { }

       protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
       {
           base.ConfigureWebHost(builder);
           builder.ConfigureServices(services =>
           {
               var descriptor = services.SingleOrDefault(
                   d => d.ServiceType == typeof(IQuotationSpreadsheetPdfConverter));
               if (descriptor is not null) services.Remove(descriptor);
               services.AddScoped<IQuotationSpreadsheetPdfConverter, FakeHandoverPdfConverter>();
           });
       }
   }

   file sealed class FakeHandoverPdfConverter : IQuotationSpreadsheetPdfConverter
   {
       private static readonly byte[] FakePdf = "%PDF-1.0 fake\n"u8.ToArray();
       public Task<byte[]> ConvertAsync(byte[] xlsxBytes, CancellationToken ct = default)
           => Task.FromResult(FakePdf);
   }
   ```

   **Lưu ý:** Test này sẽ FAIL vì controller/renderer chưa tồn tại. Đó là expected.

2. **Chạy test để confirm FAIL:**
   ```
   dotnet test backend/tests/OrderMgmt.IntegrationTests/ \
     --filter "HandoverExportTests" \
     -e TEST_DB_CONNECTION="<test-db-connection-string>"
   ```
   Expected: FAIL với 404 Not Found hoặc compile error (endpoint chưa có).

### Task 3.3 — Implement `HandoverExcelRenderer`

1. **Tạo file** `backend/src/OrderMgmt.Infrastructure/Excel/HandoverExcelRenderer.cs`

2. **Implement class** — dựa trên constants đã xác định từ Pre-task. Pattern tương tự `QuotationExcelRenderer`:

   ```csharp
   using ClosedXML.Excel;
   using OrderMgmt.Application.Sales.Quotations.Interfaces;
   using OrderMgmt.Application.Sales.Quotations.Models;

   namespace OrderMgmt.Infrastructure.Excel;

   public class HandoverExcelRenderer : IHandoverExcelRenderer
   {
       // === CẬP NHẬT các constants này dựa trên layout thực tế của template ===
       // Xem pre-task: mở file templete_bbbg.xlsx và templete_bbbg_sl.xlsx
       private const int FirstSampleRow = <ROW_FROM_TEMPLATE>;
       private const int SampleRowCount = <COUNT_FROM_TEMPLATE>;
       private const int MaxColumn = <COLUMN_COUNT_FROM_TEMPLATE>; // số cột có nội dung (dùng cho CopyRowStyle)
       // ====================================================================

       public Task<byte[]> RenderAsync(
           QuotationDto quotation,
           string templatePath,
           bool withPrice,
           CancellationToken ct = default)
       {
           var resolved = ResolveAbsolutePath(templatePath);
           using var workbook = new XLWorkbook(resolved);
           var ws = workbook.Worksheet(1);

           FillHeader(ws, quotation);
           FillItemRows(ws, quotation, withPrice);

           if (withPrice)
               FillTotals(ws, FirstSampleRow + quotation.Lines.Count, quotation);

           using var ms = new MemoryStream();
           workbook.SaveAs(ms);
           return Task.FromResult(ms.ToArray());
       }

       private static void FillHeader(IXLWorksheet ws, QuotationDto q)
       {
           // === CẬP NHẬT địa chỉ ô dựa trên template thực tế ===
           // ws.Cell("XX").SetValue(q.Code);
           // ws.Cell("XX").SetValue(q.DeliveryDate != null ? FormatDate(q.DeliveryDate.Value) : "");
           // ws.Cell("XX").SetValue(q.CustomerName);
           // ws.Cell("XX").SetValue(q.DeliveryAddress ?? string.Empty);
           throw new NotImplementedException("Cập nhật cell addresses từ template thực tế");
       }

       private static void FillItemRows(IXLWorksheet ws, QuotationDto q, bool withPrice)
       {
           var lines = q.Lines.OrderBy(l => l.SortOrder).ToList();
           int n = lines.Count;
           int lastSampleRow = FirstSampleRow + SampleRowCount - 1;

           if (n > SampleRowCount)
           {
               int extra = n - SampleRowCount;
               ws.Row(lastSampleRow + 1).InsertRowsAbove(extra);
               for (int i = 0; i < extra; i++)
                   CopyRowStyle(ws, FirstSampleRow, lastSampleRow + 1 + i);
           }
           else if (n < SampleRowCount)
           {
               for (int r = lastSampleRow; r >= FirstSampleRow + n; r--)
                   ws.Row(r).Delete();
           }

           for (int i = 0; i < n; i++)
               FillItemRow(ws, FirstSampleRow + i, i + 1, lines[i], withPrice);
       }

       private static void FillItemRow(IXLWorksheet ws, int row, int index, QuotationLineDto line, bool withPrice)
       {
           // === CẬP NHẬT số cột dựa trên template thực tế ===
           // ws.Cell(row, 1).SetValue(index);           // STT
           // ws.Cell(row, 2).SetValue(line.ProductName); // Tên hàng
           // ws.Cell(row, 3).SetValue(line.UnitName);    // DVT
           // ws.Cell(row, 4).SetValue((double)line.Quantity); // Số lượng
           // if (withPrice)
           // {
           //     ws.Cell(row, 5).SetValue((double)line.UnitPrice);  // Đơn giá
           //     ws.Cell(row, 6).SetValue((double)line.LineTotal);  // Thành tiền
           // }
           throw new NotImplementedException("Cập nhật cell addresses từ template thực tế");
       }

       private static void FillTotals(IXLWorksheet ws, int summaryRow, QuotationDto q)
       {
           // === CẬP NHẬT theo layout template có tiền ===
           // ws.Cell(summaryRow + <offset>, <col>).SetValue((double)q.AdvancePayment);
           throw new NotImplementedException("Cập nhật cell addresses từ template thực tế");
       }

       private static void CopyRowStyle(IXLWorksheet ws, int sourceRow, int targetRow)
       {
           for (int col = 1; col <= MaxColumn; col++)
           {
               var src = ws.Cell(sourceRow, col);
               var dst = ws.Cell(targetRow, col);
               dst.Style.Font.FontName = src.Style.Font.FontName;
               dst.Style.Font.FontSize = src.Style.Font.FontSize;
               dst.Style.Font.Bold = src.Style.Font.Bold;
               dst.Style.Fill.BackgroundColor = src.Style.Fill.BackgroundColor;
               dst.Style.Border.TopBorder = src.Style.Border.TopBorder;
               dst.Style.Border.BottomBorder = src.Style.Border.BottomBorder;
               dst.Style.Border.LeftBorder = src.Style.Border.LeftBorder;
               dst.Style.Border.RightBorder = src.Style.Border.RightBorder;
               dst.Style.Border.TopBorderColor = src.Style.Border.TopBorderColor;
               dst.Style.Border.BottomBorderColor = src.Style.Border.BottomBorderColor;
               dst.Style.Border.LeftBorderColor = src.Style.Border.LeftBorderColor;
               dst.Style.Border.RightBorderColor = src.Style.Border.RightBorderColor;
               dst.Style.Alignment.Horizontal = src.Style.Alignment.Horizontal;
               dst.Style.Alignment.Vertical = src.Style.Alignment.Vertical;
               dst.Style.NumberFormat.Format = src.Style.NumberFormat.Format;
           }
           ws.Row(targetRow).Height = ws.Row(sourceRow).Height;
       }

       private static string ResolveAbsolutePath(string path)
       {
           var p = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
           if (!File.Exists(p))
               throw new InvalidOperationException($"Handover Excel template not found: {p}");
           return p;
       }
   }
   ```

   **Quan trọng:** Thay thế tất cả `throw new NotImplementedException` và `<PLACEHOLDER>` bằng giá trị thực tế từ Pre-task (mở template để xác nhận).

### Task 3.4 — Đăng ký `HandoverExcelRenderer` trong DI

1. **Mở** `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs`

2. **Thêm dòng** sau `services.AddScoped<IQuotationExcelRenderer, QuotationExcelRenderer>()`:

   ```csharp
   services.AddScoped<IHandoverExcelRenderer, HandoverExcelRenderer>();
   ```

3. **Thêm using** nếu cần: `using OrderMgmt.Application.Sales.Quotations.Interfaces;`

4. **Build Infrastructure:**
   ```
   dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
   ```
   Expected: 0 errors.

5. **Commit:**
   ```
   git commit -m "feat: add HandoverExcelRenderer for biên bản bàn giao export"
   ```

## Verification

- `dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj` → 0 errors
- `HandoverExcelRenderer` không còn `NotImplementedException` hay placeholder
- DI registration thêm thành công

## Exit Criteria

- Interface `IHandoverExcelRenderer` tồn tại trong Application layer
- `HandoverExcelRenderer` implement interface đầy đủ với cell addresses thực tế
- Đăng ký trong DI
- Test file `HandoverExportTests.cs` tạo và compile thành công (dù test sẽ fail vì chưa có endpoint)
