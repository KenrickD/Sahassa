using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text.RegularExpressions;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_RawMaterial;
using WMS.Domain.DTOs.GIV_RawMaterial.Web;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto.Web;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.RawMaterial;

namespace WMS.WebApp.Controllers
{
    public class RawMaterialController : Controller
    {
        private readonly ILogger<RawMaterialController> _logger;
        private IRawMaterialService _rawMaterialService;
        private readonly IWarehouseService _warehouseService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILocationService _locationservice;
        private readonly IGeneralCodeService _generalCodeService;
        private readonly IContainerService _containerService;
        private readonly ToastService _toastService;
        public RawMaterialController(ILogger<RawMaterialController> logger,
            IRawMaterialService rawMaterialService, ICurrentUserService currentUserService, ToastService toastService, IWarehouseService warehouseService, ILocationService locationservice, IGeneralCodeService generalCodeService, IContainerService containerService)
        {
            _logger = logger;
            this._rawMaterialService = rawMaterialService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _warehouseService = warehouseService;
            _locationservice = locationservice;
            _generalCodeService = generalCodeService;
            _containerService = containerService;
        }
        [HttpPost]
        public async Task<IActionResult> GetRawMaterials(DataTablesRequest request)
        {
            _logger.LogDebug("Getting raw materials: SearchTerm={SearchTerm}, Start={Start}, Length={Length}",
                request.Search?.Value, request.Start, request.Length);

            try
            {
                var result = await _rawMaterialService.GetPaginatedRawMaterials(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
                );

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
                });

                _logger.LogDebug("Retrieved {Count} raw materials out of {Total}", result.Items.Count, result.TotalCount);

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
                _logger.LogError(ex, "Error retrieving raw materials");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load raw materials"
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetRawMaterialsByBatch(DataTablesRequest request)
        {
            _logger.LogDebug("Getting raw materials by batch: SearchTerm={SearchTerm}, Start={Start}, Length={Length}",
                request.Search?.Value, request.Start, request.Length);
            try
            {
                var result = await _rawMaterialService.GetPaginatedRawMaterialsByBatch(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
                );

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
                });

                _logger.LogDebug("Retrieved {Count} batched raw materials out of {Total}", result.Items.Count, result.TotalCount);

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
                _logger.LogError(ex, "Error retrieving raw materials by batch");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load raw materials by batch"
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Datatable()
        {
            var rawMaterials = await _rawMaterialService.GetRawMaterialsAsync();
            ViewBag.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
            return View(rawMaterials); 
        }
        [HttpGet]
        public async Task<IActionResult> GetRawMaterialTotals()
        {
            try
            {
                var totals = await _rawMaterialService.GetRawMaterialTotals();
                return Json(totals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving raw material totals");
                return Json(new { error = "Failed to load raw material totals" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm)
        {
            try
            {
                _logger.LogDebug("Exporting raw materials to Excel: SearchTerm={SearchTerm}", searchTerm);

                // Get the data for export
                var exportData = await _rawMaterialService.GetRawMaterialsForExport(searchTerm);

                // Use EPPlus to create an Excel file
                ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");
                using var package = new ExcelPackage();
                
                var worksheet = package.Workbook.Worksheets.Add("Raw Materials");

                // Add headers
                worksheet.Cells[1, 1].Value = "Material No";
                worksheet.Cells[1, 2].Value = "Description";
                worksheet.Cells[1, 3].Value = "Received Date";
                worksheet.Cells[1, 4].Value = "Total Pallets";
                worksheet.Cells[1, 5].Value = "MHU";

                // Style the header row
                using (var range = worksheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Add data
                int row = 2;
                foreach (var item in exportData)
                {
                    worksheet.Cells[row, 1].Value = item.MaterialNo;
                    worksheet.Cells[row, 2].Value = item.Description;
                    worksheet.Cells[row, 3].Value = item.ReceivedDate;
                    worksheet.Cells[row, 4].Value = item.TotalPallets;
                    worksheet.Cells[row, 5].Value = item.PalletCodes;

                    // Format date column
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "yyyy-MM-dd";

                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Set a generous width for the pallet codes column
                worksheet.Column(5).Width = 50;

                // Generate the file
                var fileContent = package.GetAsByteArray();
                var fileName = $"RawMaterials_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                _logger.LogInformation("Excel export completed with {Count} rows", exportData.Count);

                // Return the Excel file
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting raw materials to Excel");
                // Return to the index page with error message
                TempData["ErrorMessage"] = "Failed to export data. Please try again.";
                return RedirectToAction("Datatable");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedReceives(Guid rawMaterialId, bool showGrouped, [FromBody] DataTablesRequest request)
        {
            try
            {
                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _rawMaterialService.GetPaginatedReceivesByRawMaterialIdAsync(
                    rawMaterialId,
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending,
                    showGrouped
                );

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
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
                _logger.LogError(ex, "Error loading receives for RawMaterialId={RawMaterialId}", rawMaterialId);
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load receives"
                });
            }
        }

        public async Task<IActionResult> Details(Guid id, bool showGrouped = false)
        {
            var rawMaterials = await _rawMaterialService.GetRawMaterialDetailsByIdAsync(id);
            ViewBag.ShowGrouped = showGrouped;
            return View("Details",rawMaterials); 
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedPallets(Guid receiveId, bool? isGrouped, Guid? groupId, [FromBody] DataTablesRequest request)
        {
            try
            {
                _logger.LogInformation("Fetching pallets for ReceiveId={ReceiveId}, IsGrouped={IsGrouped}, GroupId={GroupId}",
                    receiveId, isGrouped, groupId);

                string searchTerm = request.Search?.Value;

                // If isGrouped and groupId aren't provided, check if this receive is part of a group
                if (!isGrouped.HasValue || !groupId.HasValue)
                {
                    var (receiveIsGrouped, receiveGroupId) = await _rawMaterialService.GetReceiveGroupInfoAsync(receiveId);
                    isGrouped = isGrouped ?? receiveIsGrouped;
                    groupId = groupId ?? receiveGroupId;

                    _logger.LogInformation("Updated group info based on receive: IsGrouped={IsGrouped}, GroupId={GroupId}",
                        isGrouped, groupId);
                }

                // Call the service method with the group information
                var result = await _rawMaterialService.GetPaginatedPalletsByReceiveIdAsync(
                    receiveId,
                    request.Start,
                    request.Length,
                    searchTerm,
                    isGrouped == true,
                    groupId
                );

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
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
                _logger.LogError(ex, "Error loading pallets for ReceiveId={ReceiveId}, IsGrouped={IsGrouped}, GroupId={GroupId}",
                    receiveId, isGrouped, groupId);

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load pallets"
                });
            }
        }
        public async Task<IActionResult> Pallets(Guid ReceiveId)
        {
            var (isGrouped, groupId) = await _rawMaterialService.GetReceiveGroupInfoAsync(ReceiveId);

            ViewBag.IsGrouped = isGrouped;
            if (groupId.HasValue)
            {
                ViewBag.GroupId = groupId.Value;
            }

            return View("Pallets", ReceiveId);
        }
        [HttpGet]
        public async Task<IActionResult> GetReceiveGroupInfo(Guid receiveId)
        {
            var (isGrouped, groupId) = await _rawMaterialService.GetReceiveGroupInfoAsync(receiveId);
            return Json(new { isGrouped, groupId });
        }
        public class PalletPhotoRequest
        {
            public string PalletId { get; set; } = default!;
        }

        [HttpGet]
        public async Task<IActionResult> ViewPhotoAttachment(Guid palletId)
        {
            var (success, message, urls) = await _rawMaterialService.GetPalletPhotoPathByIdAsync(palletId);

            if (!success || urls == null || !urls.Any())
                return PartialView("_NoPhotoFoundPartial", message);

            return PartialView("_PalletPhotoPartial", new RM_PalletPhotoViewModel { PhotoUrl = urls });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedItems(Guid receiveId, bool grouped, bool? isGrouped, Guid? groupId, [FromBody] DataTablesRequest request)
        {
            if (grouped)
            {
                var result = await _rawMaterialService.GetPaginatedGroupedItemsByReceive(
                    receiveId,
                    request.Start,
                    request.Length,
                    isGrouped ?? false,
                    groupId
                );

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
                });

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Items
                });
            }
            else
            {
                var result = await _rawMaterialService.GetPaginatedItemsByReceive(
                    receiveId,
                    request.Start,
                    request.Length,
                    isGrouped ?? false,
                    groupId
                );

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
                });

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Items
                });
            }
        }

        public async Task<IActionResult> Items(Guid receiveId, bool grouped = false)
        {
            var (isGrouped, groupId) = await _rawMaterialService.GetReceiveGroupInfoAsync(receiveId);

            if (grouped)
            {
                var groupedItems = await _rawMaterialService.GetGroupedItemsByReceive(receiveId, isGrouped, groupId);
                var model = new RawMaterialItemsViewModel
                {
                    ReceiveId = receiveId,
                    ShowGrouped = true,
                    GroupedItems = groupedItems,
                    IsGroupedReceive = isGrouped,
                    GroupId = groupId
                };
                return View(model);
            }
            else
            {
                var items = await _rawMaterialService.GetItemsByReceive(receiveId, isGrouped, groupId);
                var model = new RawMaterialItemsViewModel
                {
                    ReceiveId = receiveId,
                    ShowGrouped = false,
                    Items = items,
                    IsGroupedReceive = isGrouped,
                    GroupId = groupId
                };
                return View(model);
            }
        }

        public async Task<IActionResult> Release(Guid rawmaterialId)
        {
            var dto = await _rawMaterialService.GetRawMaterialReleaseDetailsAsync(rawmaterialId);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromBody] RawMaterialReleaseSubmitDto dto)
        {
            _logger.LogInformation("Submitting raw material release for MaterialId: {RawMaterialId} by user {UserId}",
        dto.RawMaterialId, _currentUserService.UserId);

            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // Check if there's at least one item ID or pallet release
            if ((dto.ItemIds == null || !dto.ItemIds.Any()) &&
                (dto.PalletReleases == null || !dto.PalletReleases.Any()))
            {
                var message = "At least one item or pallet must be selected for release.";
                _logger.LogWarning(message);

                if (isAjax)
                    return BadRequest(new { success = false, message });

                ModelState.AddModelError("", message);
                var vm = await _rawMaterialService.GetRawMaterialReleaseDetailsAsync(dto.RawMaterialId);
                return View("Release", vm);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Invalid model state: {Errors}", string.Join("; ", errors));

                if (isAjax)
                    return BadRequest(new { success = false, message = errors.FirstOrDefault() ?? "Invalid input", errors });

                var viewModelInvalid = await _rawMaterialService.GetRawMaterialReleaseDetailsAsync(dto.RawMaterialId);
                return View("Release", viewModelInvalid);
            }

            try
            {
                var result = await _rawMaterialService.ReleaseRawMaterialAsync(dto, _currentUserService.UserId);

                if (result.Success)
                {
                    if (isAjax)
                        return Json(new { success = true, message = "Release successful." });

                    TempData["SuccessMessage"] = "Raw material released successfully.";
                    _toastService.AddSuccessToast("Raw material released successfully.");
                    return RedirectToAction(nameof(Datatable));
                }

                if (isAjax)
                    return BadRequest(new { success = false, message = result.ErrorMessage ?? "Release failed." });

                ModelState.AddModelError("", result.ErrorMessage ?? "Release failed.");
                _toastService.AddErrorToast(result.ErrorMessage ?? "Release failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during release of MaterialId: {RawMaterialId} by user {UserId}",
                    dto.RawMaterialId, _currentUserService.UserId);

                if (isAjax)
                    return BadRequest(new { success = false, message = $"Exception during release: {ex.Message}" });

                ModelState.AddModelError("", $"Exception: {ex.Message}");
                _toastService.AddErrorToast($"Exception: {ex.Message}");
            }

            var viewModel = await _rawMaterialService.GetRawMaterialReleaseDetailsAsync(dto.RawMaterialId);
            return View("Release", viewModel);
        }


        public async Task<IActionResult> EditItem(Guid hu, Guid receiveId, bool grouped)
        {
            bool hasEditAccess = _currentUserService.HasPermission("RawMaterial.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("RawMaterial.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Raw Material - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }
            var item = await _rawMaterialService.GetItemById(hu);
            if (item == null) return NotFound();
            ViewBag.ReceiveId = receiveId;
            ViewBag.Grouped = grouped;
            return View(item);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(RM_ReceivePalletItemDto dto, Guid receiveId, bool grouped)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the validation errors.";
                ViewBag.ReceiveId = receiveId;
                ViewBag.Grouped = grouped;
                return View(dto);
            }

            var result = await _rawMaterialService.UpdateItemAsync(dto);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                if (result.Errors.Any())
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }

                ViewBag.ReceiveId = receiveId;
                ViewBag.Grouped = grouped;
                return View(dto);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Items", new { receiveId, grouped });
        }
        [HttpGet]
        public async Task<IActionResult> EditPallet(Guid id,Guid receiveId)
        {
            bool hasEditAccess = _currentUserService.HasPermission("RawMaterial.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("RawMaterial.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Raw Material - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            var (details, locationList) = await _rawMaterialService.GetReceivePalletForEditAsync(id);

            if (details == null)
                return NotFound();

            var editDto = new RM_ReceivePalletEditDto
            {
                Id = details.Id,
                PalletCode = details.PalletCode,
                HandledBy = details.HandledBy,
                StoredBy = details.StoredBy,
                PackSize = details.PackSize,
                IsReleased = details.IsReleased,
                LocationName = details.LocationName,
                ReceiveId = receiveId,
                LocationId = details.Location?.Id
            };

            ViewBag.ReceiveId = receiveId;
            ViewBag.Locations = locationList;

            return View("EditPallet", editDto);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPallet(RM_ReceivePalletEditDto model)
        {
            if (!ModelState.IsValid)
            {
                // collect all model‐state errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(ApiResponseDto<string>.ErrorResult(
                    "Validation failed.",
                    errors
                ));
            }

            var result = await _rawMaterialService.UpdatePalletAsync(model);
            return Json(result);
        }

        // This action displays the form for editing a raw material.
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            bool hasEditAccess = _currentUserService.HasPermission("RawMaterial.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("RawMaterial.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Raw Material - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }
            var dto = await _rawMaterialService.GetRawMaterialForEditAsync(id);
            if (dto == null)
                return NotFound();

            return View("EditRawMaterial",dto);
        }

        // This action handles the form submission for editing a raw material.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RawMaterialEditDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _rawMaterialService.UpdateRawMaterialAsync(dto, _currentUserService.GetCurrentUsername);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(dto);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("DataTable");
        }

        // This action displays the form for editing a receive record.
        [HttpGet]
        public async Task<IActionResult> EditReceive(Guid id, Guid rawMaterialId, bool showGrouped = false)
        {
            bool hasEditAccess = _currentUserService.HasPermission("RawMaterial.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("RawMaterial.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Raw Material - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }
            var dto = await _rawMaterialService.GetEditDtoAsync(id);
            if (dto == null)
                return NotFound();
            ViewBag.RawMaterialId = rawMaterialId;
            ViewBag.ShowGrouped = showGrouped;
            return View(dto);
        }

        // This action handles the form submission for editing a receive record.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReceive(RM_ReceiveEditDto dto, Guid rawMaterialId, bool showGrouped = false)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RawMaterialId = rawMaterialId;
                ViewBag.ShowGrouped = showGrouped;
                return View(dto);
            }

            var result = await _rawMaterialService.UpdateReceiveAsync(dto, _currentUserService.GetCurrentUsername);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                ViewBag.RawMaterialId = rawMaterialId;
                ViewBag.ShowGrouped = showGrouped;
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Details", new { id = rawMaterialId, showGrouped });
        }
        //import
        [HttpGet]
        public async Task<IActionResult> Import()
        {
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            ViewBag.Warehouses = warehouses
            .Where(w => w.IsActive)
            .Select(w => new SelectListItem
            {
                Value = w.Id.ToString(),
                Text = $"{w.Code} - {w.Name}"
            })
            .ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file, Guid warehouseId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid Excel file.";
                return RedirectToAction("Import");
            }

            try
            {
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                ViewBag.Warehouses = warehouses
                    .Where(w => w.IsActive)
                    .Select(w => new SelectListItem
                    {
                        Value = w.Id.ToString(),
                        Text = $"{w.Code} - {w.Name}"
                    }).ToList();

                var result = await _rawMaterialService.ImportRawMaterialsAsync(file, warehouseId);
                ViewBag.ImportResult = result;

                ViewBag.SuccessMessage = result.Success
                    ? $"Import completed successfully. {result.SuccessCount} row(s) imported."
                    : "Import completed with errors.";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed unexpectedly.");
                TempData["ErrorMessage"] = "Unexpected error during import.";
                return RedirectToAction("Import");
            }
        }
        [HttpGet]
        public IActionResult DownloadImportTemplate()
        {
            ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template");

            // Header row
            worksheet.Cells[1, 1].Value = "MaterialNo";
            worksheet.Cells[1, 2].Value = "Description";
            worksheet.Cells[1, 3].Value = "BatchNo";
            worksheet.Cells[1, 4].Value = "ReceivedDate";
            worksheet.Cells[1, 5].Value = "ReceivedBy";
            worksheet.Cells[1, 6].Value = "ReceiveRemarks";
            worksheet.Cells[1, 7].Value = "TransportType"; // Enum: Container or Lorry

            worksheet.Cells[1, 8].Value = "PalletCode";
            worksheet.Cells[1, 9].Value = "HandledBy";
            worksheet.Cells[1, 10].Value = "LocationId"; // Guid
            worksheet.Cells[1, 11].Value = "StoredBy";
            worksheet.Cells[1, 12].Value = "PackSize";

            worksheet.Cells[1, 13].Value = "ItemCode";
            worksheet.Cells[1, 14].Value = "ItemBatchNo";
            worksheet.Cells[1, 15].Value = "ProdDate";
            worksheet.Cells[1, 16].Value = "DG"; // true / false
            worksheet.Cells[1, 17].Value = "ItemRemarks";

            worksheet.Cells[1, 18].Value = "PhotoFile"; // Filename

            // Example rows
            worksheet.Cells[2, 1].Value = "RM-001";
            worksheet.Cells[2, 2].Value = "Citric Acid";
            worksheet.Cells[2, 3].Value = "BN-123";
            worksheet.Cells[2, 4].Value = DateTime.Today.ToString("yyyy-MM-dd");
            worksheet.Cells[2, 5].Value = "John";
            worksheet.Cells[2, 6].Value = "Received in good condition";
            worksheet.Cells[2, 7].Value = "Container";

            worksheet.Cells[2, 8].Value = "PLT-001";
            worksheet.Cells[2, 9].Value = "Jane";
            worksheet.Cells[2, 10].Value = ""; // Leave blank or insert actual GUID if known
            worksheet.Cells[2, 11].Value = "System";
            worksheet.Cells[2, 12].Value = 10;

            worksheet.Cells[2, 13].Value = "ITEM-001";
            worksheet.Cells[2, 14].Value = "BN-ITEM-001";
            worksheet.Cells[2, 15].Value = DateTime.Today.ToString("yyyy-MM-dd");
            worksheet.Cells[2, 16].Value = false;
            worksheet.Cells[2, 17].Value = "No remarks";

            worksheet.Cells[2, 18].Value = "photo1.jpg";

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = "RawMaterialFullImportTemplate.xlsx";
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(stream, contentType, fileName);
        }
        [HttpGet]
        public IActionResult Report()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(string ReportMode, DateTime? cutoffDate, DateTime? startMonth, DateTime? endMonth)
        {
            if (ReportMode == "Weekly" && cutoffDate.HasValue)
            {
                var cutoff = DateTime.SpecifyKind(cutoffDate.Value, DateTimeKind.Utc);
                var (fileContent, fileName) = await _rawMaterialService.GenerateRawMaterialWeeklyExcelAsync(cutoff);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            if (ReportMode == "Monthly" && startMonth.HasValue && endMonth.HasValue)
            {
                var start = DateTime.SpecifyKind(startMonth.Value, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endMonth.Value, DateTimeKind.Utc);
                var (fileContent, fileName) = await _rawMaterialService.GenerateRawMaterialExcelAsync(start, end);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            TempData["Error"] = "Invalid input. Please select the appropriate date range.";
            return RedirectToAction("Datatable");
        }
        [HttpGet]
        public async Task<IActionResult> GetAllLocations()
        {
            var locations = await _locationservice.GetAllLocationsAsync();

            // Optionally filter only active locations
            var activeLocations = locations
                .Where(loc => loc.IsActive)
                .Select(loc => new
                {
                    loc.Id,
                    Display = !string.IsNullOrWhiteSpace(loc.Barcode) ? loc.Barcode : loc.Code
                })
                .ToList();

            return Json(activeLocations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLocationInline(UpdatePalletInlineLocationDto dto)
        {
            bool hasEditAccess = _currentUserService.HasPermission("RawMaterial.Write");

            if (!hasEditAccess)
            {
                _logger.LogWarning("User {UserId} tried to update pallet location without permission.", _currentUserService.UserId);
                return Forbid();
            }

            var result = await _rawMaterialService.UpdatePalletLocationAsync(
                dto.PalletId,
                dto.LocationId,
                _currentUserService.GetCurrentUsername);

            return result.Success ? Json(result) : BadRequest(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGroupFieldInline(Guid palletId, string fieldName, bool value)
        {
            if (!_currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE))
            {
                return Forbid();
            }

            var success = await _rawMaterialService.UpdatePalletGroupFieldAsync(
                palletId,
                fieldName,
                value,
                _currentUserService.GetCurrentUsername);

            if (success)
            {
                return Ok();
            }
            else
            {
                return StatusCode(500, "Failed to update group field");
            }
        }

        #region Create Raw Material Web
        [HttpGet]
        public async Task<IActionResult> CreateRawMaterial()
        {

            var packageCodes = await _generalCodeService
                .GetCodesForDropdownAsync(AppConsts.GeneralCodeType.PRODUCT_TYPE);

            ViewBag.PackageTypes = packageCodes
                .OrderBy(c => c.Sequence)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();
            var containers = await _containerService.GetAllContainerAsync();
            ViewBag.Containers = containers
                .Select(c => new SelectListItem
                {
                    Value = c.ContainerId.ToString(),
                    Text = c.ContainerNo_GW
                })
                .ToList();
            return View(); 
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [FromForm] RawMaterialCreateWebDto dto,
    [FromForm] List<IFormFile> photoFiles,
    [FromForm] List<string> photoPalletCodes)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // 1) Validate model state
            if (!ModelState.IsValid)
            {
                // Gather all field‐level errors
                var modelErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .SelectMany(kvp => kvp.Value.Errors
                        .Select(err => new { Field = kvp.Key, Error = err.ErrorMessage }))
                    .ToList();

                // Log them in one line
                var errorDescriptions = string.Join("; ", modelErrors
                    .Select(e => $"{e.Field}: {e.Error}"));
                _logger.LogWarning(
                    "CreateRawMaterial model validation failed for user {User}. Errors: {Errors}",
                    _currentUserService.GetCurrentUsername,
                    errorDescriptions
                );

                // If AJAX, return JSON
                if (isAjax)
                    return Json(new
                    {
                        success = false,
                        message = "Validation failed.",
                        errors = modelErrors.Select(e => e.Error).ToArray()
                    });

                // Otherwise fall back to view
                ViewData["ValidationErrors"] = modelErrors;
                return View("CreateRawMaterial", dto);
            }

            // 2) Ensure files and codes line up
            if (photoFiles.Count != photoPalletCodes.Count)
            {
                const string mismatchError = "Mismatch between uploaded photo files and pallet codes.";
                _logger.LogWarning(
                    "CreateRawMaterial mismatch photoFiles vs photoPalletCodes for user {User}: {Error}",
                    _currentUserService.GetCurrentUsername,
                    mismatchError
                );

                if (isAjax)
                    return Json(new { success = false, message = mismatchError, errors = new[] { mismatchError } });

                ModelState.AddModelError(string.Empty, mismatchError);
                return View("CreateRawMaterial", dto);
            }

            // 3) Pair files with pallet codes
            var photoDtos = photoFiles
                .Select((file, i) => new RM_ReceivePalletPhotoWebUploadDto
                {
                    PalletCode = photoPalletCodes[i],
                    PhotoFile = file
                })
                .ToList();

            // 4) Delegate to service
            var result = await _rawMaterialService.CreateRawMaterialFromWebAsync(
                dto,
                photoDtos,
                _currentUserService.GetCurrentUsername,
                _currentUserService.CurrentWarehouseId
            );

            // 5) Return JSON for AJAX
            if (isAjax)
            {
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    errors = result.Errors
                });
            }

            // 6) Non-AJAX fallback
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Datatable");
            }

            // 7) On error, re-populate ModelState and log
            TempData["ErrorMessage"] = result.Message;
            if (result.Errors != null)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err);
            }
            _logger.LogError(
                "CreateRawMaterialFromWebAsync failed for MaterialNo={MaterialNo}, user={User}: {Error}",
                dto.MaterialNo,
                _currentUserService.GetCurrentUsername,
                result.Message
            );

            return View("CreateRawMaterial", dto);
        }




        #endregion

        #region Get Raw Material Web
        [HttpGet]
        public async Task<IActionResult> GetBatchNumbers()
        {
            try
            {
                var batchNumbers = await _rawMaterialService.GetDistinctBatchNumbersAsync();
                return Json(batchNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batch numbers");
                return Json(new List<string>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMaterialNumbers()
        {
            try
            {
                var materialNumbers = await _rawMaterialService.GetDistinctMaterialNumbersAsync();
                return Json(materialNumbers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving material numbers");
                return Json(new List<string>());
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetRawMaterialsByPallet()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["order[0][column]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int sortColumnIndex = sortColumn != null ? Convert.ToInt32(sortColumn) : 0;
                bool sortAscending = sortColumnDirection == "asc";

                var result = await _rawMaterialService.GetPaginatedRawMaterialsByPallet(
                    searchValue ?? string.Empty, skip, pageSize, sortColumnIndex, sortAscending);

                // Set HasEditAccess for each item
                foreach (var item in result.Items)
                {
                    item.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
                }

                return Json(new
                {
                    draw = draw,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated raw materials by pallet");
                return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPalletCodes()
        {
            try
            {
                var palletCodes = await _rawMaterialService.GetPalletCodesAsync();
                return Json(palletCodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pallet codes");
                return Json(new List<string>());
            }
        }
        #endregion
        #region Edit Raw Material Web
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRawMaterialGroupField(Guid rawMaterialId, string fieldName, bool value)
        {
            if (!_currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE))
            {
                return Forbid();
            }

            var success = await _rawMaterialService.UpdateRawMaterialGroupFieldAsync(
                rawMaterialId,
                fieldName,
                value,
                _currentUserService.GetCurrentUsername);

            if (success)
            {
                return Ok(new { success = true, message = "Group field updated successfully" });
            }
            else
            {
                return StatusCode(500, new { success = false, message = "Failed to update group field" });
            }
        }
        #endregion

        [HttpPost]
        public async Task<IActionResult> ProcessPendingReleases(CancellationToken cancellation = default)
        {
            try
            {
                // Create a session key that's unique for today
                string sessionKey = $"ProcessedReleases_{DateTime.Now:yyyyMMdd}";

                // Check if we've already processed releases today
                if (HttpContext.Session.GetString(sessionKey) == null)
                {
                    await _rawMaterialService.ProcessRawMaterialReleases(DateTime.UtcNow, cancellation);

                    // Set the session flag to prevent processing again today
                    HttpContext.Session.SetString(sessionKey, "Processed");

                    return Json(new { success = true, message = "Pending releases processed successfully" });
                }

                return Json(new { success = true, message = "Releases already processed today" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending releases");
                return Json(new { success = false, message = "Error processing pending releases", error = ex.Message });
            }
        }
        // Add these methods to your RawMaterialController class

        [HttpGet]
        public async Task<IActionResult> Releases(Guid id)
        {
            var rawMaterial = await _rawMaterialService.GetRawMaterialDetailsByIdAsync(id);
            if (rawMaterial == null)
            {
                return NotFound();
            }

            return View("Releases", rawMaterial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedReleases(Guid rawMaterialId, [FromBody] DataTablesRequest request)
        {
            try
            {
                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _rawMaterialService.GetPaginatedReleasesByRawMaterialIdAsync(
                    rawMaterialId,
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending
                );

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAWMATERIAL_WRITE);
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
                _logger.LogError(ex, "Error loading releases for RawMaterialId={RawMaterialId}", rawMaterialId);
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load releases"
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ReleaseDetails(Guid id, Guid rawMaterialId)
        {
            var releaseDetails = await _rawMaterialService.GetReleaseDetailsByIdAsync(id);
            if (releaseDetails == null)
            {
                return NotFound();
            }

            ViewBag.RawMaterialId = rawMaterialId;
            return View("ReleaseDetails", releaseDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedReleaseDetails(Guid releaseId, [FromBody] DataTablesRequest request)
        {
            try
            {
                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _rawMaterialService.GetPaginatedReleaseDetailsAsync(
                    releaseId,
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending
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
                _logger.LogError(ex, "Error loading release details for ReleaseId={ReleaseId}", releaseId);
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load release details"
                });
            }
        }

        #region JobRelease

        [HttpGet]
        public async Task<IActionResult> JobReleases()
        {
            _logger.LogInformation("Accessing Job Releases page for user {UserId}", _currentUserService.UserId);

            // Check permissions
            if (!_currentUserService.HasPermission("RawMaterial.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to Job Releases - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedJobReleases([FromBody] DataTablesRequest request)
        {
            try
            {
                // Check permissions
                if (!_currentUserService.HasPermission("RawMaterial.Read"))
                {
                    return Forbid();
                }

                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _rawMaterialService.GetPaginatedJobReleasesAsync(
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending
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
                _logger.LogError(ex, "Error loading job releases");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load job releases"
                });
            }
        }

        [HttpGet]
        [Route("RawMaterial/JobReleaseDetails/{jobId:guid}")]
        public async Task<IActionResult> JobReleaseDetails(Guid jobId)
        {
            _logger.LogInformation("Accessing Job Release Details for JobId {JobId} by user {UserId}",
                jobId, _currentUserService.UserId);

            // Check permissions
            if (!_currentUserService.HasPermission("RawMaterial.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to Job Release Details - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            var jobReleaseDetails = await _rawMaterialService.GetJobReleaseDetailsByJobIdAsync(jobId);
            if (jobReleaseDetails == null)
            {
                _logger.LogWarning("Job release not found for JobId: {JobId}", jobId);
                return NotFound();
            }

            return View("JobReleaseDetails", jobReleaseDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedJobReleaseDetails(Guid jobId, [FromBody] DataTablesRequest request)
        {
            try
            {
                // Check permissions
                if (!_currentUserService.HasPermission("RawMaterial.Read"))
                {
                    return Forbid();
                }

                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _rawMaterialService.GetPaginatedJobReleaseIndividualReleasesAsync(
                    jobId,
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending
                );

                // Add edit access information
                result.Items.ForEach(x =>
                {
                    // You can add edit access logic here if needed
                    // x.HasEditAccess = _currentUserService.HasPermission("RawMaterial.Write");
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
                _logger.LogError(ex, "Error loading job release details for JobId={JobId}", jobId);
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load job release details"
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> CreateJobRelease()
        {
            _logger.LogInformation("Accessing Create Job Release page for user {UserId}", _currentUserService.UserId);

            // Check permissions
            if (!_currentUserService.HasPermission("RawMaterial.Write"))
            {
                _logger.LogWarning("User {UserId} denied access to Create Job Release - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            return View("CreateJobRelease");
        }
        [HttpGet]
        public async Task<IActionResult> GetAvailableMaterialsForJobRelease()
        {
            try
            {
                _logger.LogInformation("Fetching available materials for job release");

                // Check permissions
                if (!_currentUserService.HasPermission("RawMaterial.Read"))
                {
                    return Forbid();
                }

                var materials = await _rawMaterialService.GetAvailableMaterialsForJobReleaseAsync();

                _logger.LogInformation("Found {Count} available materials for job release", materials.Count);
                return Json(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available materials for job release");
                return Json(new { error = "Failed to load available materials" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetMaterialInventoryForJobRelease([FromBody] MaterialInventoryRequestDto request)
        {
            try
            {
                _logger.LogInformation("Fetching material inventory for {Count} materials", request.MaterialIds.Count);

                // Check permissions
                if (!_currentUserService.HasPermission("RawMaterial.Read"))
                {
                    return Forbid();
                }

                if (request.MaterialIds == null || !request.MaterialIds.Any())
                {
                    return BadRequest(new { error = "No material IDs provided" });
                }

                var inventoryData = await _rawMaterialService.GetMaterialInventoryForJobReleaseAsync(request.MaterialIds);

                _logger.LogInformation("Retrieved inventory data for {Count} materials", inventoryData.Count);
                return Json(inventoryData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching material inventory for job release");
                return Json(new { error = "Failed to load material inventory" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitJobRelease([FromBody] JobReleaseCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Submitting job release for user {UserId} with {MaterialCount} materials",
                    _currentUserService.UserId, dto.Materials?.Count ?? 0);

                // Check permissions
                if (!_currentUserService.HasPermission("RawMaterial.Write"))
                {
                    _logger.LogWarning("User {UserId} denied access to submit job release - insufficient permissions",
                        _currentUserService.UserId);
                    return Forbid();
                }

                // Validate input
                if (dto.Materials == null || !dto.Materials.Any())
                {
                    return BadRequest(new { success = false, message = "No materials provided for release" });
                }

                // Validate that each material has at least one receive with items
                foreach (var material in dto.Materials)
                {
                    if (!material.Receives.Any())
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Material {material.MaterialNo} has no receives selected"
                        });
                    }

                    foreach (var receive in material.Receives)
                    {
                        if (!receive.Pallets.Any() && !receive.Items.Any())
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = $"Material {material.MaterialNo} has receives with no items selected"
                            });
                        }

                        // Validate release date
                        if (receive.ReleaseDate.Date < DateTime.UtcNow.Date)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = $"Release date cannot be in the past for material {material.MaterialNo}"
                            });
                        }
                    }
                }

                // Create the job release
                var result = await _rawMaterialService.CreateJobReleaseAsync(dto, _currentUserService.UserId);

                if (result.Success)
                {
                    _logger.LogInformation("Job release created successfully");

                    return Json(new
                    {
                        success = true,
                        message = "Job release created successfully"
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to create job release: {ErrorMessage}", result.ErrorMessage);
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage ?? "Failed to create job release"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while submitting job release for user {UserId}",
                    _currentUserService.UserId);

                return BadRequest(new
                {
                    success = false,
                    message = "An unexpected error occurred while creating the job release"
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("RawMaterial/ExportJobReleaseToExcel/{jobId:guid}")]
        public async Task<IActionResult> ExportJobReleaseToExcel(Guid jobId)
        {
            try
            {
                _logger.LogInformation("Exporting Job Release to Excel for JobId {JobId} by user {UserId}",
                    jobId, _currentUserService.UserId);

                // Check permissions
                if (!_currentUserService.HasPermission("RawMaterial.Read"))
                {
                    _logger.LogWarning("User {UserId} denied access to Export Job Release - insufficient permissions",
                        _currentUserService.UserId);
                    return Forbid();
                }

                var (fileContent, fileName) = await _rawMaterialService.ExportJobReleaseToExcelAsync(jobId);

                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning("No data found for JobId: {JobId}", jobId);
                    TempData["ErrorMessage"] = "No data found for export.";
                    return RedirectToAction("JobReleaseDetails", new { jobId });
                }

                _logger.LogInformation("Successfully exported Job Release for JobId {JobId}, file size: {FileSize} bytes",
                    jobId, fileContent.Length);

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Job Release to Excel for JobId {JobId}", jobId);
                TempData["ErrorMessage"] = "An error occurred while exporting to Excel.";
                return RedirectToAction("JobReleaseDetails", new { jobId });
            }
        }
        #endregion
    }
}
