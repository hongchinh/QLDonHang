using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Catalog.Customers.Interfaces;
using OrderMgmt.Application.Catalog.Customers.Services;
using OrderMgmt.Application.Catalog.Lookups.Interfaces;
using OrderMgmt.Application.Catalog.Lookups.Services;
using OrderMgmt.Application.Catalog.Products.Interfaces;
using OrderMgmt.Application.Catalog.Products.Services;
using OrderMgmt.Application.Identity.Admin.Interfaces;
using OrderMgmt.Application.Identity.Admin.Services;
using OrderMgmt.Application.Identity.Interfaces;
using OrderMgmt.Application.Identity.Services;
using OrderMgmt.Application.Identity.UserSettings.Interfaces;
using OrderMgmt.Application.Identity.UserSettings.Services;
using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
using OrderMgmt.Application.Reports.SalesRevenue.Services;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Services;

namespace OrderMgmt.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Auto-scan IRegister implementations for Mapster.
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(assembly);
        services.AddSingleton(typeAdapterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICatalogLookupService, CatalogLookupService>();
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<IQuotationDashboardService, QuotationDashboardService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IUserQuotationSettingsService, UserQuotationSettingsService>();
        services.AddScoped<IQuotationBulkTransferService, QuotationBulkTransferService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<ISalesRevenueReportService, SalesRevenueReportService>();

        return services;
    }
}
