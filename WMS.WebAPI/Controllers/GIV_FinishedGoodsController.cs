using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_FinishedGood;
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
