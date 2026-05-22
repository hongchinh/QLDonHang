using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Infrastructure.Excel;

public class QuotationExportPathResolver : IQuotationExportPathResolver
{
    private readonly IOptionsMonitor<QuotationExportOptions> _options;
    private readonly IAppDbContext _db;

    public QuotationExportPathResolver(IOptionsMonitor<QuotationExportOptions> options, IAppDbContext db)
    {
        _options = options;
        _db = db;
    }

    public async Task<string> ResolveTemplatePathAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;
        var userTemplateFile = await _db.UserQuotationSettings
            .AsNoTracking()
            .Where(s => s.UserId == ownerUserId)
            .Select(s => s.TemplateFileName)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(userTemplateFile))
        {
            var userPath = Path.Combine(ResolveAbsolute(opts.UserTemplatesPath), userTemplateFile);
            if (File.Exists(userPath)) return userPath;
        }

        return ResolveAbsolute(opts.TemplatePath);
    }

    public Task<string> ResolveHandoverTemplatePathAsync(
        Guid ownerUserId,
        QuotationTemplateType type,
        CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;

        var (userFileName, systemPath) = type switch
        {
            QuotationTemplateType.HandoverWithPrice =>
                ($"{ownerUserId}_handover_with_price.xlsx", opts.HandoverWithPriceTemplatePath),
            QuotationTemplateType.HandoverNoPrice =>
                ($"{ownerUserId}_handover_no_price.xlsx", opts.HandoverNoPriceTemplatePath),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        var userDir = ResolveAbsolute(opts.UserTemplatesPath);
        var userPath = Path.Combine(userDir, userFileName);
        if (File.Exists(userPath)) return Task.FromResult(userPath);

        return Task.FromResult(ResolveAbsolute(systemPath));
    }

    public string GetUserTemplatesDirectory()
    {
        var dir = ResolveAbsolute(_options.CurrentValue.UserTemplatesPath);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string ResolveAbsolute(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
}
