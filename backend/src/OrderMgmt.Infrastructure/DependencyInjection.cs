using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Identity.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Infrastructure.Excel;
using OrderMgmt.Infrastructure.Identity;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.Infrastructure.Persistence.Seed;
using OrderMgmt.Infrastructure.Services;

namespace OrderMgmt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            // Read at resolution time so WebApplicationFactory overrides (in-memory config added
            // after AddInfrastructure is called) actually take effect.
            var cfg = sp.GetRequiredService<IConfiguration>();
            var cs = cfg.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException(
                    "Missing connection string 'Default'. Configure via environment variable " +
                    "ConnectionStrings__Default, user-secrets, or appsettings.{Environment}.json.");
            options.UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations"));
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.Configure<RefreshTokenOptions>(configuration.GetSection(RefreshTokenOptions.SectionName));
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.Configure<QuotationExportOptions>(configuration.GetSection(QuotationExportOptions.SectionName));
        services.AddScoped<IQuotationExcelRenderer, QuotationExcelRenderer>();
        services.AddScoped<IQuotationSpreadsheetPdfConverter, LibreOfficeSpreadsheetPdfConverter>();

        return services;
    }
}
