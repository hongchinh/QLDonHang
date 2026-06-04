using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Application.Reports.SalesRevenue.Interfaces;

public interface IRevenueReportExcelRenderer
{
    (byte[] Bytes, string FileName) Render(
        List<SalesRevenueLineItemDto> items,
        DateTime from,
        DateTime to);
}
