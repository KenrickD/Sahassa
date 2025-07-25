using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.Interfaces;
using WMS.WebApp.Models.DataTables;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.WebApp.Controllers
{
    public class ContainerController : Controller
    {
        private readonly ILogger<ContainerController> _logger;
        private IContainerService _containerService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        public ContainerController(ILogger<ContainerController> logger,
            IContainerService containerService, ICurrentUserService currentUserService, ToastService toastService)
        {
            _logger = logger;
            this._containerService = containerService;
            _currentUserService = currentUserService;
            _toastService = toastService;
        }
        // ADD these methods to your existing ContainerController class

        [HttpGet]
        [Route("Container/Import")]
        public IActionResult Import()
        {
            ViewBag.ContainerType = "import";
            ViewBag.HasEditAccess = _currentUserService.HasPermission("Container.Write");
            ViewBag.HasDeleteAccess = _currentUserService.HasPermission("Container.Delete");
            ViewBag.HasViewAccess = _currentUserService.HasPermission("Container.Read");
            return View("DataTable"); // Use the same view
        }

        [HttpGet]
        [Route("Container/Export")]
        public IActionResult Export()
        {
            ViewBag.ContainerType = "export";
            ViewBag.HasEditAccess = _currentUserService.HasPermission("Container.Write");
            ViewBag.HasDeleteAccess = _currentUserService.HasPermission("Container.Delete");
            ViewBag.HasViewAccess = _currentUserService.HasPermission("Container.Read");
            return View("DataTable"); // Use the same view
        }

        // MODIFY your existing DataTable method to support type parameter
        public IActionResult DataTable(string type = "import")
        {
            ViewBag.ContainerType = type;
            ViewBag.HasEditAccess = _currentUserService.HasPermission("Container.Write");
            ViewBag.HasDeleteAccess = _currentUserService.HasPermission("Container.Delete");
            ViewBag.HasViewAccess = _currentUserService.HasPermission("Container.Read");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedContainers([FromForm] DataTablesRequest request, [FromForm] string containerType = "import")
        {
            try
            {
                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                // Determine process type based on containerType parameter
                var processType = containerType.ToLower() == "export" ? ContainerProcessType.Export : ContainerProcessType.Import;

                _logger.LogInformation("Loading {ContainerType} containers with ProcessType={ProcessType}", containerType, processType);

                var result = await _containerService.GetPaginatedContainersByTypeAsync(
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending,
                    processType
                );

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load paginated {ContainerType} containers.", containerType);
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = $"Failed to load {containerType} container data"
                });
            }
        }
        [HttpGet]
        [Route("Container/GetPhotoUrls/{containerId:guid}")]
        [ProducesResponseType(typeof(ApiResponseDto<List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPhotoUrls(Guid containerId)
        {
            var result = await _containerService.GetContainerPhotoUrlsAsync(containerId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Perform update operation to Container Data
        /// </summary>
        /// <param name="containerDto">The editable fields</param>
        /// <param name="Username">Who made the change</param>
        /// <param name="WhFlag">Warehouse flag</param>
        /// <returns>Container ID</returns>
        /// <response code="200">Update operation successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="500">Uncaught Exception</response>
        [Authorize]
        [HttpPost("/Container/Update")]
        [EnableRateLimiting("AuthPolicy")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponseDto<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateContainer(
    [FromForm] ContainerUpdateDto containerDto,
    [FromForm] string Username)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponseDto<object>.ErrorResult("Validation failed", errors));
            }

            var result = await _containerService.UpdateContainerAsync(containerDto, _currentUserService.GetCurrentUsername);

            if (!result.Success)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResult(result.Message));
            }

            return Ok(ApiResponseDto<string>.SuccessResult(result.Data, "Operation successful."));
        }
        [HttpDelete("Container/DeletePhoto/{photoId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePhoto(Guid photoId)
        {
            var result = await _containerService.DeleteContainerPhotoAsync(photoId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
        [HttpPost("Container/ReplacePhoto/{photoId:guid}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplacePhoto(Guid photoId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponseDto<object>.ErrorResult("No file uploaded."));

            var result = await _containerService.ReplaceContainerPhotoAsync(photoId, file);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
        [HttpGet("Container/DownloadReport/{containerId:guid}")]
        [Authorize]
        public async Task<IActionResult> DownloadReport(Guid containerId,CancellationToken cancellationToken)
        {
            var result = await _containerService.GeneratePdfReportAsync(containerId,cancellationToken);

            if (!result.Success)
                return BadRequest(result);
            var (fileContent, fileName) = result.Data;
            return File(fileContent, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> GetContainerPallets(
             Guid containerId,
             int page = 1,
             int pageSize = 10,
             string? searchTerm = null)
        {
            try
            {
                _logger.LogInformation("Getting container pallets for container {ContainerId}", containerId);

                var result = await _containerService.GetContainerPalletsAsync(containerId, page, pageSize, searchTerm);

                return Json(new
                {
                    success = true,
                    pallets = result.Pallets,
                    totalCount = result.TotalCount,
                    currentPage = result.CurrentPage,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages,
                    containerNo = result.ContainerNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting container pallets for container {ContainerId}", containerId);
                return Json(new { success = false, error = "Failed to load container pallets" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPalletItems(Guid palletId)
        {
            try
            {
                _logger.LogInformation("Getting pallet items for pallet {PalletId}", palletId);

                var items = await _containerService.GetPalletItemsAsync(palletId);

                return Json(new
                {
                    success = true,
                    items = items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pallet items for pallet {PalletId}", palletId);
                return Json(new { success = false, error = "Failed to load pallet items" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetContainerAttachments(Guid containerId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting container attachments for jobId {ContainerId}", containerId);

                if (containerId == Guid.Empty)
                {
                    _logger.LogWarning("Invalid jobId provided: {ContainerId}", containerId);
                    return Json(new { success = false, error = "Invalid job ID provided" });
                }

                var attachments = await _containerService.GetExternalContainerInfoAttachmentByIdAsync(containerId, cancellationToken);

                if (attachments == null)
                {
                    _logger.LogWarning("No attachments found for containerId {ContainerId}", containerId);
                    return Json(new
                    {
                        success = true,
                        attachments = new List<object>(),
                        message = "No attachments found for this container"
                    });
                }

                _logger.LogInformation("Retrieved {Count} attachments for containerId {ContainerId}", attachments.Count, containerId);

                return Json(new
                {
                    success = true,
                    attachments = attachments.Select(a => new
                    {
                        fileName = a.FileName,
                        filePath = a.FilePath,
                        attachmentType = a.AttachmentType,
                        attachmentReference = a.AttachmentReference,
                        downloadSignedUrl = a.DownloadSignedUrl
                    }).ToList()
                });
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                _logger.LogWarning("Request cancelled for containerId {ContainerId}", containerId);
                return Json(new { success = false, error = "Request was cancelled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting container attachments for containerId {ContainerId}", containerId);
                return Json(new { success = false, error = "Failed to load container attachments" });
            }
        }
    }
}
