using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Entities.Catalog;
using OrderMgmt.Domain.Entities.Sales;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Sales.Quotations.Services;

public class QuotationService : IQuotationService
{
    private const string CodePrefix = "BG";
    private const int MaxCreateAttempts = 5;

    // (current, action) -> next status
    private static readonly Dictionary<(QuotationStatus, QuotationAction), QuotationStatus> Transitions = new()
    {
        { (QuotationStatus.Draft, QuotationAction.Send), QuotationStatus.Sent },
        { (QuotationStatus.Draft, QuotationAction.Cancel), QuotationStatus.Cancelled },
        { (QuotationStatus.Sent, QuotationAction.Confirm), QuotationStatus.Confirmed },
        { (QuotationStatus.Sent, QuotationAction.Cancel), QuotationStatus.Cancelled },
        { (QuotationStatus.Confirmed, QuotationAction.Cancel), QuotationStatus.Cancelled },
    };

    private readonly IAppDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IQuotationPdfRenderer _pdfRenderer;

    public QuotationService(
        IAppDbContext db,
        IDateTime clock,
        ICurrentUser currentUser,
        IQuotationPdfRenderer pdfRenderer)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _pdfRenderer = pdfRenderer;
    }

    public async Task<PagedResult<QuotationListItemDto>> ListAsync(QuotationListRequest request, CancellationToken ct = default)
    {
        var query = _db.Quotations
            .AsNoTracking()
            .Where(q => !q.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(q => q.Status == request.Status.Value);
        if (request.CustomerId.HasValue)
            query = query.Where(q => q.CustomerId == request.CustomerId.Value);
        if (request.From.HasValue)
            query = query.Where(q => q.QuotationDate >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(q => q.QuotationDate <= request.To.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLike(request.Search.Trim())}%";
            query = query.Where(q =>
                EF.Functions.ILike(q.Code, pattern)
                || EF.Functions.ILike(q.CustomerName, pattern));
        }

        query = (request.SortBy?.ToLowerInvariant(), request.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc") => query.OrderByDescending(q => q.Code),
            ("code", _) => query.OrderBy(q => q.Code),
            ("date", "desc") => query.OrderByDescending(q => q.QuotationDate),
            ("date", _) => query.OrderBy(q => q.QuotationDate),
            ("total", "desc") => query.OrderByDescending(q => q.Total),
            ("total", _) => query.OrderBy(q => q.Total),
            _ => query.OrderByDescending(q => q.CreatedAt),
        };

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuotationListItemDto
            {
                Id = q.Id,
                Code = q.Code,
                QuotationDate = q.QuotationDate,
                CustomerName = q.CustomerName,
                ContactPhone = q.ContactPhone,
                Total = q.Total,
                Status = q.Status,
                CreatedByName = q.CreatedBy.HasValue
                    ? _db.Users.Where(u => u.Id == q.CreatedBy).Select(u => u.FullName).FirstOrDefault()
                    : null,
                CreatedAt = q.CreatedAt,
            })
            .ToListAsync(ct);

        return new PagedResult<QuotationListItemDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
        };
    }

    public async Task<QuotationDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var quotation = await _db.Quotations
            .AsNoTracking()
            .Include(q => q.Lines.OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Quotation), id);

        return MapToDto(quotation);
    }

    public async Task<QuotationDto> CreateAsync(UpsertQuotationRequest request, CancellationToken ct = default)
    {
        var customer = await EnsureCustomerAsync(request.CustomerId, ct);

        for (var attempt = 1; attempt <= MaxCreateAttempts; attempt++)
        {
            var code = await GenerateCodeAsync(ct);

            if (await _db.Quotations.AnyAsync(q => q.Code == code, ct))
                continue;

            var quotation = new Quotation
            {
                Code = code,
                QuotationDate = request.QuotationDate,
                CustomerId = customer.Id,
                CustomerName = string.IsNullOrWhiteSpace(request.CustomerName)
                    ? customer.Name
                    : request.CustomerName.Trim(),
                CustomerTaxCode = customer.TaxCode,
                CustomerAddress = customer.CompanyAddress,
                ContactPerson = customer.ContactPerson,
                ContactPhone = customer.PhoneNumber,
                DeliveryAddress = request.DeliveryAddress ?? customer.DefaultShippingAddress,
                DeliveryRecipient = request.DeliveryRecipient,
                DeliveryPhone = request.DeliveryPhone,
                DeliveryDate = request.DeliveryDate,
                DeliveryNote = request.DeliveryNote,
                TaxRate = request.TaxRate,
                Discount = request.Discount,
                Freight = request.Freight,
                InternalNote = request.InternalNote,
                Status = QuotationStatus.Draft,
            };

            await PopulateLinesAsync(quotation, request.Lines, isNew: true, ct);
            Recompute(quotation);
            _db.Quotations.Add(quotation);

            try
            {
                await _db.SaveChangesAsync(ct);
                return await GetAsync(quotation.Id, ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex) && attempt < MaxCreateAttempts)
            {
                _db.Entry(quotation).State = EntityState.Detached;
                foreach (var line in quotation.Lines)
                    _db.Entry(line).State = EntityState.Detached;
            }
        }

        throw new ConflictException("Không thể tạo mã báo giá tự động sau nhiều lần thử. Vui lòng thử lại.");
    }

    public async Task<QuotationDto> UpdateAsync(Guid id, UpsertQuotationRequest request, CancellationToken ct = default)
    {
        var quotation = await _db.Quotations
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Quotation), id);

        if (quotation.Status == QuotationStatus.Cancelled)
            throw new DomainException("CONFLICT", "Báo giá đã hủy không thể chỉnh sửa.");

        var customer = await EnsureCustomerAsync(request.CustomerId, ct);

        quotation.QuotationDate = request.QuotationDate;
        quotation.CustomerId = customer.Id;
        quotation.CustomerName = string.IsNullOrWhiteSpace(request.CustomerName)
            ? customer.Name
            : request.CustomerName.Trim();
        quotation.CustomerTaxCode = customer.TaxCode;
        quotation.CustomerAddress = customer.CompanyAddress;
        quotation.ContactPerson = customer.ContactPerson;
        quotation.ContactPhone = customer.PhoneNumber;
        quotation.DeliveryAddress = request.DeliveryAddress;
        quotation.DeliveryRecipient = request.DeliveryRecipient;
        quotation.DeliveryPhone = request.DeliveryPhone;
        quotation.DeliveryDate = request.DeliveryDate;
        quotation.DeliveryNote = request.DeliveryNote;
        quotation.TaxRate = request.TaxRate;
        quotation.Discount = request.Discount;
        quotation.Freight = request.Freight;
        quotation.InternalNote = request.InternalNote;

        await PopulateLinesAsync(quotation, request.Lines, isNew: false, ct);
        Recompute(quotation);

        await _db.SaveChangesAsync(ct);
        return await GetAsync(quotation.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var quotation = await _db.Quotations
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Quotation), id);

        quotation.IsDeleted = true;
        quotation.DeletedAt = _clock.UtcNow;
        quotation.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<(byte[] Pdf, string FileName)> RenderPdfAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await GetAsync(id, ct);
        var bytes = _pdfRenderer.Render(dto);
        var fileName = $"BaoGia_{dto.Code}.pdf";
        return (bytes, fileName);
    }

    public async Task<QuotationDto> TransitionAsync(Guid id, QuotationAction action, CancellationToken ct = default)
    {
        var quotation = await _db.Quotations
            .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Quotation), id);

        if (!Transitions.TryGetValue((quotation.Status, action), out var next))
            throw new DomainException("CONFLICT", $"Không thể chuyển trạng thái '{quotation.Status}' bằng hành động '{action}'.");

        quotation.Status = next;
        await _db.SaveChangesAsync(ct);
        return await GetAsync(quotation.Id, ct);
    }

    private async Task<Customer> EnsureCustomerAsync(Guid customerId, CancellationToken ct)
    {
        return await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Customer), customerId);
    }

    private async Task PopulateLinesAsync(
        Quotation quotation,
        IReadOnlyList<UpsertQuotationLineRequest> requestedLines,
        bool isNew,
        CancellationToken ct)
    {
        var productIds = requestedLines
            .Where(l => l.ProductId.HasValue)
            .Select(l => l.ProductId!.Value)
            .Distinct()
            .ToList();

        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : await _db.Products
                .Include(p => p.Unit)
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, ct);

        if (isNew)
        {
            foreach (var line in requestedLines)
            {
                var entity = new QuotationLine();
                ApplyLine(entity, line, products);
                quotation.Lines.Add(entity);
            }
            return;
        }

        var existing = quotation.Lines.ToDictionary(l => l.Id);
        var keptIds = new HashSet<Guid>();

        foreach (var line in requestedLines)
        {
            if (line.Id.HasValue && existing.TryGetValue(line.Id.Value, out var entity))
            {
                ApplyLine(entity, line, products);
                keptIds.Add(entity.Id);
            }
            else
            {
                var newLine = new QuotationLine();
                ApplyLine(newLine, line, products);
                quotation.Lines.Add(newLine);
            }
        }

        foreach (var existingLine in existing.Values)
        {
            if (keptIds.Contains(existingLine.Id)) continue;
            existingLine.IsDeleted = true;
            existingLine.DeletedAt = _clock.UtcNow;
            existingLine.DeletedBy = _currentUser.UserId;
        }
    }

    private static void ApplyLine(QuotationLine entity, UpsertQuotationLineRequest req, Dictionary<Guid, Product> products)
    {
        entity.SortOrder = req.SortOrder;
        entity.ProductId = req.ProductId;
        entity.PricingMode = req.PricingMode;

        if (req.ProductId.HasValue && products.TryGetValue(req.ProductId.Value, out var product))
        {
            entity.ProductCode = product.Code;
            entity.ProductName = string.IsNullOrWhiteSpace(req.ProductName) ? product.Name : req.ProductName;
            entity.Specification = req.Specification ?? product.Specification;
            entity.UnitName = string.IsNullOrWhiteSpace(req.UnitName) ? (product.Unit?.Name ?? string.Empty) : req.UnitName;
            entity.PricingMode = req.PricingMode == default ? product.PricingMode : req.PricingMode;
        }
        else
        {
            entity.ProductCode = req.ProductCode;
            entity.ProductName = req.ProductName;
            entity.Specification = req.Specification;
            entity.UnitName = req.UnitName;
        }

        entity.Length = req.Length;
        entity.Width = req.Width;
        entity.Thickness = req.Thickness;
        entity.Density = req.Density;
        entity.SheetCount = req.SheetCount;

        entity.Quantity = req.Quantity;
        entity.UnitPrice = req.UnitPrice;
        entity.UnitCost = req.UnitCost;
        entity.Note = req.Note;
    }

    private static void Recompute(Quotation q)
    {
        decimal subtotal = 0m;
        decimal totalCost = 0m;

        foreach (var line in q.Lines.Where(l => !l.IsDeleted))
        {
            line.LineTotal = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
            if (line.UnitCost.HasValue)
            {
                line.LineCost = Math.Round(line.Quantity * line.UnitCost.Value, 2, MidpointRounding.AwayFromZero);
                line.LineProfit = line.LineTotal - line.LineCost.Value;
            }
            else
            {
                line.LineCost = null;
                line.LineProfit = null;
            }
            subtotal += line.LineTotal;
            totalCost += line.LineCost ?? 0m;
        }

        q.Subtotal = subtotal;
        q.TotalCost = totalCost;
        q.TaxAmount = Math.Round(subtotal * q.TaxRate / 100m, 0, MidpointRounding.AwayFromZero);
        q.Total = subtotal - q.Discount + q.Freight + q.TaxAmount;
        // Gross profit excludes freight (it's a pass-through) and includes the customer discount.
        q.GrossProfit = subtotal - totalCost - q.Discount;
    }

    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var date = _clock.Now.ToString("yyMMdd");
        var todayCount = await _db.Quotations
            .CountAsync(q => q.Code.StartsWith($"{CodePrefix}-{date}"), ct);
        return $"{CodePrefix}-{date}-{todayCount + 1:D4}";
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        var sqlState = inner?.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
        return sqlState == "23505";
    }

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static QuotationDto MapToDto(Quotation q) => new()
    {
        Id = q.Id,
        Code = q.Code,
        QuotationDate = q.QuotationDate,
        CustomerId = q.CustomerId,
        CustomerName = q.CustomerName,
        CustomerTaxCode = q.CustomerTaxCode,
        CustomerAddress = q.CustomerAddress,
        ContactPerson = q.ContactPerson,
        ContactPhone = q.ContactPhone,
        DeliveryAddress = q.DeliveryAddress,
        DeliveryRecipient = q.DeliveryRecipient,
        DeliveryPhone = q.DeliveryPhone,
        DeliveryDate = q.DeliveryDate,
        DeliveryNote = q.DeliveryNote,
        Subtotal = q.Subtotal,
        Discount = q.Discount,
        Freight = q.Freight,
        TaxRate = q.TaxRate,
        TaxAmount = q.TaxAmount,
        Total = q.Total,
        TotalCost = q.TotalCost,
        GrossProfit = q.GrossProfit,
        Status = q.Status,
        InternalNote = q.InternalNote,
        CreatedAt = q.CreatedAt,
        CreatedBy = q.CreatedBy,
        Lines = q.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new QuotationLineDto
            {
                Id = l.Id,
                SortOrder = l.SortOrder,
                ProductId = l.ProductId,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                Specification = l.Specification,
                UnitName = l.UnitName,
                PricingMode = l.PricingMode,
                Length = l.Length,
                Width = l.Width,
                Thickness = l.Thickness,
                Density = l.Density,
                SheetCount = l.SheetCount,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                UnitCost = l.UnitCost,
                LineCost = l.LineCost,
                LineProfit = l.LineProfit,
                Note = l.Note,
            })
            .ToList(),
    };
}
