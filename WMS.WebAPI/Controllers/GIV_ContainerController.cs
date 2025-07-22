using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Application.Interfaces;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_ContainerController : Controller
    {
        private IContainerService ContainerService { get; }
        private readonly ILogger<GIV_ContainerController> _logger;
        public GIV_ContainerController(ILogger<GIV_ContainerController> logger, IContainerService ContainerService)
        {
            this._logger = logger;
            this.ContainerService = ContainerService;
        }
        /// <summary>
        /// Peform Delta operation to Container Data
        /// </summary>
        /// <param name="request">Container Information,Username,WhFlag</param>
        /// <returns>Container ID</returns>
        /// <response code="200">Delta Operation successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="500">Uncaught Exception</response>
        [HttpPost]
        [Authorize]
        [Route("Create")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateContainer(ContainerCreateDto containerDto, string Username, bool WhFlag)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var id = await ContainerService.DeltaUpdateContainerAsync(containerDto, Username, WhFlag);
            return Ok(new ApiResponseDto<Guid>
            {
                Success = true,
                Message = "Operation successful.",
                Data = id
            });
        }
        /// <summary>
        /// Peform update operation to Container Data
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Container ID</returns>
        /// <response code="200">Update Operation successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="500">Uncaught Exception</response>
        [HttpPost]
        [Authorize]
        [Route("Update")]
        [EnableRateLimiting("AuthPolicy")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponseDto<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateContainer([FromForm] ContainerUpdateDto containerDto, [FromForm] string Username)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResult("Validation failed",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var result = await ContainerService.UpdateContainerAsync(containerDto, Username);
            if (!result.Success)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResult(result.Message));
            }
            return Ok(ApiResponseDto<string>.SuccessResult(result.Data, "Operation successful."));
        }

        /// <summary>
        /// Get All Container Data
        /// </summary>
        /// <returns>All Active Container ID</returns>
        /// <response code="200">get data successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="429">Too many requests</response>
        /// <response code="409">Operation Not Allowed</response>
        /// <response code="500">Uncaught Exception</response>
        /// <response code="408">Request Timeout</response>
        [HttpGet]
        [Route("GetAllContainer")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<Containers>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status408RequestTimeout)]
        public async Task<IActionResult> GetAllContainer()
        {
            var Containers = await ContainerService.GetContainersByProcessTypeAsync(ContainerProcessType.Import);

            return Ok(Containers);
        }

        /// <summary>
        /// Get All Container Data
        /// </summary>
        /// <returns>All Active Container ID</returns>
        /// <response code="200">get data successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="429">Too many requests</response>
        /// <response code="409">Operation Not Allowed</response>
        /// <response code="500">Uncaught Exception</response>
        /// <response code="408">Request Timeout</response>
        [HttpGet]
        [Route("GetAllExportContainers")]
        [Authorize]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<Containers>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status408RequestTimeout)]
        public async Task<IActionResult> GetAllExportContainers()
        {
            var Containers = await ContainerService.GetContainersByProcessTypeAsync(ContainerProcessType.Export);

            return Ok(Containers);
        }
    }
}
