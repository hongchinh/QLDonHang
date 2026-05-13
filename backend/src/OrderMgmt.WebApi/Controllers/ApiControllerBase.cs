using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data) => Ok(ApiResponse<T>.Ok(data));
    protected ActionResult<ApiResponse> Success() => Ok(ApiResponse.Ok());
}
