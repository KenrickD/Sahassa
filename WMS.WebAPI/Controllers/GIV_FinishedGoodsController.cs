using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.DTOs.GIV_Invoicing;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using static WMS.Application.Helpers.WhatsappHelper;

namespace WMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_FinishedGoodsController : Controller
    {
        private IFinishedGoodService FinishedGoodService { get; }
        private readonly ILogger<GIV_FinishedGoodsController> _logger;
        public GIV_FinishedGoodsController(ILogger<GIV_FinishedGoodsController> logger, IFinishedGoodService FinishedGoodService)
        {
            this._logger = logger;
            this.FinishedGoodService = FinishedGoodService;
        }
        /// <summary>
        /// Create Finished Good
        /// </summary>
        /// <returns>200</returns>
        /// <response code="200">get data successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Data Not Found</response>
        /// <response code="409">Operation Not Allowed</response>
        /// <response code="500">Uncaught Exception</response>
        [HttpPost]
        [Authorize]
        [Route("Create")]
        [EnableRateLimiting("AuthPolicy")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponseDto<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromForm] List<FG_ReceiveCreateDto> receivingDtos, [FromForm] List<FG_ReceivePalletPhotoUploadDto> photos,[FromForm] string Username, [FromForm] Guid warehouseid)
        {
            var result = await FinishedGoodService.CreateFinishedGoodAsync(receivingDtos, photos, Username, warehouseid);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        [HttpGet]
        [Route("GetGroupedPalletCount")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<List<GroupedPalletCountDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status408RequestTimeout)]
        public async Task<IActionResult> GetGroupedPalletCount(string dateString)
        {
            try
            {
                string format = "dd-MM-yyyy HH:mm:ss";
                DateTime cutoffDate = DateTime.ParseExact(dateString, format, System.Globalization.CultureInfo.InvariantCulture);
                cutoffDate = DateTime.SpecifyKind(cutoffDate, DateTimeKind.Utc);
                var result = await FinishedGoodService.GetGroupedPalletCount(cutoffDate);
                var response = ApiResponseDto<List<GroupedPalletCountDto>>.SuccessResult(result);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get grouped pallet count.");
                var errorResponse = ApiResponseDto<List<GroupedPalletCountDto>>.ErrorResult("Failed to get grouped pallet count.", new List<string> { ex.Message });
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Update Finished Good Pallet Location
        /// </summary>
        /// <returns>FinishedGood Pallet ID</returns>
        /// <response code="200">get data successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">Data Not Found</response>
        /// <response code="409">Operation Not Allowed</response>
        /// <response code="500">Uncaught Exception</response>

    }
}
