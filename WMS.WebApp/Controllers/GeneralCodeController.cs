// GeneralCodeController.cs
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Extensions;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.Clients;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.GeneralCodes;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class GeneralCodeController : Controller
    {
        private readonly IGeneralCodeService _generalCodeService;
        private readonly IWarehouseService _warehouseService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILogger<GeneralCodeController> _logger;

        public GeneralCodeController(
            IGeneralCodeService generalCodeService,
            IWarehouseService warehouseService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IMapper mapper,
            ILogger<GeneralCodeController> logger)
        {
            _generalCodeService = generalCodeService;
            _warehouseService = warehouseService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Views
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.GENERAL_TYPE_CODE_READ}")]
        public IActionResult Index()
        {
            _logger.LogInformation("General codes index page accessed by user {UserId}", _currentUserService.UserId);
            return View();
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.GENERAL_TYPE_CODE_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> CreateCodeType()
        {
            var viewModel = new GeneralCodeTypePageViewModel
            {
                CodeType = new GeneralCodeTypeViewModel(),
                HasEditAccess = _currentUserService.HasPermission("GeneralCode.Write")
            };

            await LoadWarehousesAsync(viewModel);
            return View("Detail", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCodeType(GeneralCodeTypeViewModel viewModel)
        {
            _logger.LogInformation("Creating code type: {Name} by user {UserId}",
                viewModel.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    var createDto = new GeneralCodeTypeCreateDto
                    {
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        WarehouseId = viewModel.WarehouseId
                    };

                    var createdCodeType = await _generalCodeService.CreateCodeTypeAsync(createDto);

                    _logger.LogInformation("Code type created successfully: {Id} - {Name} by user {UserId}",
                        createdCodeType.Id, createdCodeType.Name, _currentUserService.UserId);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Code type created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating code type: {Name} by user {UserId}",
                        viewModel.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error creating code type: {ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                    _toastService.AddErrorToast($"{ex.Message}");
                }
            }
            else if (Request.IsAjaxRequest())
            {
                return BadRequest(new { success = false, errors = ModelState });
            }

            var pageViewModel = new GeneralCodeTypePageViewModel
            {
                CodeType = viewModel,
                HasEditAccess = _currentUserService.HasPermission("GeneralCode.Write")
            };
            await LoadWarehousesAsync(pageViewModel);

            return View("Detail", pageViewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.GENERAL_TYPE_CODE_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> EditCodeType(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("Edit code type accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            bool hasEditAccess = _currentUserService.HasPermission("GeneralCode.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("GeneralCode.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit code type {Id} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var codeType = await _generalCodeService.GetCodeTypeByIdAsync(id.Value);

                var viewModel = new GeneralCodeTypePageViewModel
                {
                    CodeType = new GeneralCodeTypeViewModel
                    {
                        Id = codeType.Id,
                        Name = codeType.Name,
                        Description = codeType.Description,
                        WarehouseId = codeType.WarehouseId,
                        WarehouseName = codeType.WarehouseName,
                        CodesCount = codeType.CodesCount,
                        CreatedAt = DateTimeExtensions.AsSingaporeTime(codeType.CreatedAt)
                    },
                    HasEditAccess = hasEditAccess,
                    IsEdit = true
                };

                await LoadWarehousesAsync(viewModel);
                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading code type {Id} for editing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load code type details");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCodeType(GeneralCodeTypeViewModel model)
        {
            _logger.LogInformation("Updating code type {Id} - {Name} by user {UserId}",
                model.Id, model.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    bool hasEditAccess = _currentUserService.HasPermission("GeneralCode.Write");

                    if (!hasEditAccess)
                    {
                        _logger.LogWarning("User {UserId} denied access to update code type {Id} - insufficient permissions",
                            _currentUserService.UserId, model.Id);
                        return Forbid();
                    }

                    var updateDto = new GeneralCodeTypeUpdateDto
                    {
                        Name = model.Name,
                        Description = model.Description
                    };

                    await _generalCodeService.UpdateCodeTypeAsync(model.Id, updateDto);

                    _logger.LogInformation("Code type updated successfully: {Id} - {Name} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    _toastService.AddSuccessToast("Code type updated successfully!");

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    return RedirectToAction("ViewCodeType", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating code type {Id} - {Name} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error updating code type: {ex.Message}");
                    _toastService.AddErrorToast($"{ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                }
            }

            var viewModel = new GeneralCodeTypePageViewModel
            {
                CodeType = model,
                HasEditAccess = _currentUserService.HasPermission("GeneralCode.Write"),
                IsEdit = true
            };
            await LoadWarehousesAsync(viewModel);

            return View("Detail", viewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.GENERAL_TYPE_CODE_READ}")]
        [HttpGet]
        public async Task<IActionResult> ViewCodeType(Guid? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            try
            {
                var codeType = await _generalCodeService.GetCodeTypeByIdAsync(id.Value);
                var codes = await _generalCodeService.GetCodesByTypeIdAsync(id.Value);

                var viewModel = new GeneralCodeTypeWithCodesViewModel
                {
                    CodeType = new GeneralCodeTypeViewModel
                    {
                        Id = codeType.Id,
                        Name = codeType.Name,
                        Description = codeType.Description,
                        WarehouseName = codeType.WarehouseName,
                        WarehouseId = codeType.WarehouseId,
                        CodesCount = codeType.CodesCount,
                        CreatedAt = DateTimeExtensions.AsSingaporeTime(codeType.CreatedAt)
                    },
                    Codes = codes.Select(c => new GeneralCodeViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Detail = c.Detail,
                        Sequence = c.Sequence,
                        GeneralCodeTypeId = c.GeneralCodeTypeId,
                        GeneralCodeTypeName = c.GeneralCodeTypeName
                    }).OrderBy(c => c.Sequence).ToList(),
                    HasEditAccess = _currentUserService.HasPermission("GeneralCode.Write")
                };

                return View("DetailWithCodes", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading code type {Id} for viewing", id.Value);
                _toastService.AddErrorToast("Failed to load code type details");
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region AJAX Data Methods

        [HttpPost]
        public async Task<IActionResult> GetCodeTypes(DataTablesRequest request)
        {
            try
            {
                var result = await _generalCodeService.GetPaginatedCodeTypesAsync(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
                );

                result.Items.ForEach(x =>
                {
                    x.HasWriteAccess = _currentUserService.HasPermission("GeneralCode.Write");
                    x.HasDeleteAccess = _currentUserService.HasPermission("GeneralCode.Delete");
                });

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
                _logger.LogError(ex, "Error retrieving code types");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load code types"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetHierarchicalData()
        {
            try
            {
                var data = await _generalCodeService.GetCodesWithTypesAsync();

                // Add permission flags
                foreach (var item in data)
                {
                    item.CodeType.HasWriteAccess = _currentUserService.HasPermission("GeneralCodeType.Write");
                    item.CodeType.HasDeleteAccess = _currentUserService.HasPermission("GeneralCodeType.Delete");

                    foreach (var code in item.Codes)
                    {
                        code.HasWriteAccess = _currentUserService.HasPermission("GeneralCode.Write");
                        code.HasDeleteAccess = _currentUserService.HasPermission("GeneralCode.Delete");
                    }
                }

                return Json(new { success = true, data});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving hierarchical data");
                return Json(new { success = false, message = "Failed to load data" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCode([FromBody] GeneralCodeCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, errors = ModelState });
                }

                var result = await _generalCodeService.CreateCodeAsync(dto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating code");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCode(Guid id, [FromBody] GeneralCodeUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, errors = ModelState });
                }

                var result = await _generalCodeService.UpdateCodeAsync(id, dto);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating code");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReorderCodes([FromBody] List<ReorderCodeRequest> reorderData)
        {
            try
            {
                var reorderList = reorderData.Select(x => (x.CodeId, x.NewSequence)).ToList();
                await _generalCodeService.ReorderCodesAsync(reorderList);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering codes");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Delete Actions

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCodeType(Guid codeTypeId)
        {
            try
            {
                await _generalCodeService.DeleteCodeTypeAsync(codeTypeId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Code type deleted successfully" });
                }

                _toastService.AddSuccessToast("Code type deleted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting code type {Id}", codeTypeId);

                var errorMessage = $"Failed to delete code type: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCode(Guid codeId)
        {
            try
            {
                await _generalCodeService.DeleteCodeAsync(codeId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Code deleted successfully" });
                }

                _toastService.AddSuccessToast("Code deleted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting code {Id}", codeId);

                var errorMessage = $"Failed to delete code: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadWarehousesAsync(GeneralCodeTypePageViewModel viewModel)
        {
            try
            {
                bool isAdmin = _currentUserService.IsInRole("SystemAdmin");
                var warehouses = await _warehouseService.GetAllWarehousesAsync();

                viewModel.Warehouses = warehouses
                    .Where(w => w.IsActive && (w.Id == _currentUserService.CurrentWarehouseId || isAdmin))
                    .Select(w => new WarehouseDropdownItem
                    {
                        Id = w.Id,
                        Name = w.Name,
                        Code = w.Code
                    })
                    .OrderBy(w => w.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouses for general code form");
                viewModel.Warehouses = new List<WarehouseDropdownItem>();
            }
        }

        #endregion
    }

    // Request models for AJAX calls
    public class ReorderCodeRequest
    {
        public Guid CodeId { get; set; }
        public int NewSequence { get; set; }
    }
}