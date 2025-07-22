using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WMS.API.Controllers;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;

namespace WMS.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_PalletController : Controller
    {
        private IPalletService PalletService { get; }
        private readonly ILogger<GIV_PalletController> _logger;
        public GIV_PalletController(ILogger<GIV_PalletController> logger, IPalletService PalletService)
        {
            this._logger = logger;
            this.PalletService = PalletService;
        }
        [HttpPost]
        [Authorize]
        [Route("UpdatePalletLocation")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePalletLocation(UpdatePalletLocationDto UpdatePalletLocationDto, string Username, Guid warehouseid)
        {
            var result = await PalletService.UpdatePalletLocation(UpdatePalletLocationDto, Username, warehouseid);
            if (result.Success)
            {

                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        [HttpPost]
        [Authorize]
        [Route("ReleasePalletLocation")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReleasePalletLocation(ReleasePalletLocationDto ReleasePalletLocationDto, string Username, Guid warehouseid)
        {
            var result = await PalletService.ReleasePalletLocation(ReleasePalletLocationDto, Username, warehouseid);
            if (result.Success)
            {

                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        [HttpPost]
        [Authorize]
        [Route("ActualReleasePalletLocation")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualReleasePalletLocation(ReleasePalletLocationDto ReleasePalletLocationDto, string Username, Guid warehouseid)
        {
            try
            {
                var result = await PalletService.ActualReleasePalletLocation(ReleasePalletLocationDto, Username, warehouseid);
                if (result.Success)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ActualReleasePalletLocation for Code: {Code}, WarehouseId: {WarehouseId}",
                    ReleasePalletLocationDto.Code, warehouseid);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDto<object>.ErrorResult("Internal server error occurred. Please contact support."));
            }
        }
        [HttpPost]
        [Authorize]
        [Route("BulkUpdatePalletLocation")]
        [EnableRateLimiting("AuthPolicy")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponseDto<BulkUpdatePalletLocationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BulkUpdatePalletLocation(
             [FromBody] BulkUpdatePalletLocationDto bulkUpdateDto,
             [FromQuery] string username,
             [FromQuery] Guid warehouseId)
        {
            try
            {
                _logger.LogInformation("Bulk update pallet location request received. Username: {Username}, WarehouseId: {WarehouseId}, PalletCount: {PalletCount}",
                    username, warehouseId, bulkUpdateDto?.PalletCodes?.Count ?? 0);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
                    return BadRequest(ApiResponseDto<object>.ErrorResult("Validation failed", errors));
                }

                var result = await PalletService.BulkUpdatePalletLocationAsync(bulkUpdateDto, username, warehouseId);

                if (result.Success)
                {
                    // Return different status codes based on the result
                    if (result.Data.FailureCount == 0)
                    {
                        // All successful
                        return Ok(result);
                    }
                    else if (result.Data.SuccessCount > 0)
                    {
                        // Partial success - some succeeded, some failed
                        return Ok(result); // Still 200 OK, but with details about failures
                    }
                    else
                    {
                        // All failed
                        return BadRequest(result);
                    }
                }
                else
                {
                    // Determine appropriate status code based on error type
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(result);
                    }
                    else if (result.Message.Contains("capacity") || result.Message.Contains("full"))
                    {
                        return Conflict(result);
                    }
                    else
                    {
                        return BadRequest(result);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in bulk update request");
                return BadRequest(ApiResponseDto<object>.ErrorResult("Invalid request parameters", new List<string> { ex.Message }));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for bulk update");
                return Unauthorized(ApiResponseDto<object>.ErrorResult("Unauthorized access", new List<string> { "Insufficient permissions" }));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during bulk update");
                return Conflict(ApiResponseDto<object>.ErrorResult("Operation conflict", new List<string> { ex.Message }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during bulk pallet location update");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error", new List<string> { "An unexpected error occurred" }));
            }
        }
    }
}
