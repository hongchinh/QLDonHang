using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.UserSettings.Interfaces;
using OrderMgmt.Application.Identity.UserSettings.Models;
using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.WebApi.Controllers;

[Authorize]
[Route("api/me/quotation-settings")]
public class MeQuotationSettingsController : ApiControllerBase
{
    private readonly IUserQuotationSettingsService _service;

    public MeQuotationSettingsController(IUserQuotationSettingsService service)
    {
        _service = service;
    }

    private static QuotationTemplateType ParseTemplateType(string? type) => type?.ToLowerInvariant() switch
    {
        null or "" or "quotation" => QuotationTemplateType.Quotation,
        "handover-with-price" => QuotationTemplateType.HandoverWithPrice,
        "handover-no-price" => QuotationTemplateType.HandoverNoPrice,
        _ => throw new BadHttpRequestException($"Unknown template type: '{type}'"),
    };

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> GetMine(CancellationToken ct)
        => Success(await _service.GetForCurrentUserAsync(ct));

    [HttpPut("template")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> UploadTemplate(
        IFormFile file,
        [FromQuery] string? type,
        CancellationToken ct)
    {
        var uploaded = new UploadedFile(
            file.FileName,
            file.ContentType,
            file.Length,
            () => file.OpenReadStream());

        var templateType = ParseTemplateType(type);

        if (templateType == QuotationTemplateType.Quotation)
            return Success(await _service.UploadTemplateAsync(uploaded, ct));
        else
            return Success(await _service.UploadHandoverTemplateAsync(uploaded, templateType, ct));
    }

    [HttpDelete("template")]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> DeleteTemplate(
        [FromQuery] string? type,
        CancellationToken ct)
    {
        var templateType = ParseTemplateType(type);

        if (templateType == QuotationTemplateType.Quotation)
            return Success(await _service.DeleteTemplateAsync(ct));
        else
            return Success(await _service.DeleteHandoverTemplateAsync(templateType, ct));
    }

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(
        [FromQuery] string? type,
        CancellationToken ct)
    {
        var templateType = ParseTemplateType(type);

        (Stream Stream, string FileName)? result;
        if (templateType == QuotationTemplateType.Quotation)
            result = await _service.GetCurrentUserTemplateStreamAsync(ct);
        else
            result = await _service.GetCurrentUserHandoverTemplateStreamAsync(templateType, ct);

        if (result is null) return NotFound();
        return File(
            result.Value.Stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.Value.FileName);
    }

    [HttpGet("effective-template")]
    public async Task<IActionResult> DownloadEffectiveTemplate(
        [FromQuery] string? type,
        [FromServices] IConfiguration configuration,
        [FromServices] IWebHostEnvironment env,
        CancellationToken ct)
    {
        var templateType = ParseTemplateType(type);

        (Stream Stream, string FileName)? result;
        if (templateType == QuotationTemplateType.Quotation)
            result = await _service.GetCurrentUserTemplateStreamAsync(ct);
        else
            result = await _service.GetCurrentUserHandoverTemplateStreamAsync(templateType, ct);

        if (result is not null)
            return File(
                result.Value.Stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Value.FileName);

        var relativePath = templateType switch
        {
            QuotationTemplateType.Quotation => configuration["QuotationExport:TemplatePath"],
            QuotationTemplateType.HandoverWithPrice => configuration["QuotationExport:HandoverWithPriceTemplatePath"],
            QuotationTemplateType.HandoverNoPrice => configuration["QuotationExport:HandoverNoPriceTemplatePath"],
            _ => null,
        };

        if (relativePath is null) return NotFound();
        var fullPath = Path.Combine(env.ContentRootPath, relativePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var stream = System.IO.File.OpenRead(fullPath);
        return File(
            stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Path.GetFileName(fullPath));
    }

    [HttpGet("default-template")]
    public IActionResult DownloadDefaultTemplate(
        [FromQuery] string? type,
        [FromServices] IConfiguration configuration,
        [FromServices] IWebHostEnvironment env)
    {
        var templateType = ParseTemplateType(type);
        var relativePath = templateType switch
        {
            QuotationTemplateType.Quotation => configuration["QuotationExport:TemplatePath"],
            QuotationTemplateType.HandoverWithPrice => configuration["QuotationExport:HandoverWithPriceTemplatePath"],
            QuotationTemplateType.HandoverNoPrice => configuration["QuotationExport:HandoverNoPriceTemplatePath"],
            _ => null,
        };

        if (relativePath is null) return NotFound();
        var fullPath = Path.Combine(env.ContentRootPath, relativePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Path.GetFileName(fullPath));
    }
}
