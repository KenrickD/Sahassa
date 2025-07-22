using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.DTOs.GIV_Container;

namespace WMS.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeneralCodeController : Controller
    {
        private readonly ILogger<GeneralCodeController> _logger;
        private readonly IGeneralCodeService _generalCodeService;
        public GeneralCodeController(ILogger<GeneralCodeController> logger, IGeneralCodeService generalCodeService)
        {
            _logger = logger;
            _generalCodeService = generalCodeService;
        }

        [HttpGet]
        [Route("GetAllGeneralCodeType")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<List<GeneralCodeTypeDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status408RequestTimeout)]
        public async Task<IActionResult> GetAllCodeTypes()
        {
            try
            {
                var result = await _generalCodeService.GetAllCodeTypesAsync();
                var response = ApiResponseDto<List<GeneralCodeTypeDto>>.SuccessResult(result);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all code types.");
                var errorResponse = ApiResponseDto<List<GeneralCodeTypeDto>>.ErrorResult("Failed to get code types.", new List<string> { ex.Message });
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet]
        [Route("GetGeneralCodeByTypeId")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<List<GeneralCodeDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status408RequestTimeout)]
        public async Task<IActionResult> GetGeneralCodeByTypeId(Guid GeneralCodeTypeId)
        {
            try
            {
                var result = await _generalCodeService.GetCodesByTypeIdAsync(GeneralCodeTypeId);
                var response = ApiResponseDto<List<GeneralCodeDto>>.SuccessResult(result);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get General code .");
                var errorResponse = ApiResponseDto<List<GeneralCodeDto>>.ErrorResult("Failed to get General code .", new List<string> { ex.Message });
                return StatusCode(500, errorResponse);
            }
        }
    }
}
