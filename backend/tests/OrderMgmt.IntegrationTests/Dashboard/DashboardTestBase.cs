using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Domain.Entities.Sales;
using OrderMgmt.Domain.Enums;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;

namespace OrderMgmt.IntegrationTests.Dashboard;

public abstract class DashboardTestBase : QuotationTestBase
{
    protected DashboardTestBase(PostgresFixture pg) : base(pg) { }

    protected async Task<Guid> SeedQuotationAsync(
        string code,
        Guid ownerUserId,
        QuotationStatus status,
        DateOnly quotationDate,
        decimal total,
        DateTime? confirmedAt = null,
        DateTime? cancelledAt = null,
        Guid? customerId = null,
        string? customerName = null,
        IEnumerable<(string ProductName, Guid? ProductId, decimal Quantity, decimal UnitPrice)>? lines = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var q = new Quotation
        {
            Code = code,
            QuotationDate = quotationDate,
            OwnerUserId = ownerUserId,
            CustomerId = customerId ?? _customerId,
            CustomerName = customerName ?? "Test Customer",
            Status = status,
            ConfirmedAt = confirmedAt,
            CancelledAt = cancelledAt,
            Total = total,
            Subtotal = total,
            TaxRate = 0,
            TaxAmount = 0,
            Discount = 0,
            Freight = 0,
        };
        foreach (var l in lines ?? Array.Empty<(string, Guid?, decimal, decimal)>())
        {
            q.Lines.Add(new QuotationLine
            {
                ProductName = l.ProductName,
                ProductId = l.ProductId,
                UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = Math.Round(l.Quantity * l.UnitPrice, 2),
            });
        }
        db.Quotations.Add(q);
        await db.SaveChangesAsync();
        return q.Id;
    }

    protected async Task<Guid> GetUserIdAsync(string username)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Username == username).Select(u => u.Id).FirstAsync();
    }
}
