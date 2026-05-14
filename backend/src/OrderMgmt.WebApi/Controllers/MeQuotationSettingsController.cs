using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.UserSettings.Interfaces;
using OrderMgmt.Application.Identity.UserSettings.Models;

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

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> GetMine(CancellationToken ct)
        => Success(await _service.GetForCurrentUserAsync(ct));

    [HttpPut("template")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> UploadTemplate(
        IFormFile file, CancellationToken ct)
    {
        var uploaded = new UploadedFile(
            file.FileName,
            file.ContentType,
            file.Length,
            () => file.OpenReadStream());
        return Success(await _service.UploadTemplateAsync(uploaded, ct));
    }

    [HttpDelete("template")]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> DeleteTemplate(CancellationToken ct)
        => Success(await _service.DeleteTemplateAsync(ct));

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken ct)
    {
        var result = await _service.GetCurrentUserTemplateStreamAsync(ct);
        if (result is null) return NotFound();
        return File(
            result.Value.Stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.Value.FileName);
    }
}
