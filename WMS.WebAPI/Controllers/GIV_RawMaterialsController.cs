using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto;
using WMS.Domain.DTOs.RawMaterial;
using WMS.Domain.DTOs.Users;
using static WMS.Application.Helpers.WhatsappHelper;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_RawMaterialsController : Controller
    {
        private IRawMaterialService RawMaterialService { get; }
        private readonly ILogger<GIV_RawMaterialsController> _logger;
        public GIV_RawMaterialsController(ILogger<GIV_RawMaterialsController> logger, IRawMaterialService RawMaterialService)
        {
            this._logger = logger;
            this.RawMaterialService = RawMaterialService;
        }
        /// <summary>
        /// Update Raw Material Create
        /// </summary>
        /// <returns>Raw Material ID</returns>
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
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
    [FromForm] RawMaterialCreateDto receivingdto,
    [FromForm] List<RM_ReceivePalletPhotoUploadDto> photos,
    [FromForm] string Username,
    [FromForm] Guid warehouseid)
        {
            var dtoJson = JsonSerializer.Serialize(receivingdto, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });
            _logger.LogInformation("[{Timestamp}] RawMaterialCreateDto JSON: {DtoJson}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC"), dtoJson);
            var result = await RawMaterialService.CreateRawMaterialAsync(receivingdto,photos,Username,warehouseid);
            if (result.Success)
            {

                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        

    }
}
