using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.DTOs.GIV_FinishedGood.Web;
using WMS.Domain.DTOs.GIV_RawMaterial;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.FinishedGood;
using WMS.WebApp.Models.FinishedGood.Pallet;
using WMS.WebApp.Models.RawMaterial;


namespace WMS.WebApp.Controllers
{
    public class FinishedGoodController : Controller
    {
        private readonly ILogger<FinishedGoodController> _logger;
        private IFinishedGoodService _finishedGoodService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILocationService _locationservice;
        public FinishedGoodController(ILogger<FinishedGoodController> logger,
            IFinishedGoodService finishedGoodService,ICurrentUserService currentUserService,ToastService toastService,
            ILocationService locationService)
        {
            _logger = logger;
            this._finishedGoodService = finishedGoodService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _locationservice = locationService;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedFinishedGoods([FromBody] DataTablesRequest request)
        {
            try
            {
                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _finishedGoodService.GetPaginatedFinishedGoodsAsync(
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending
                );

                var grandTotals = await _finishedGoodService.GetFinishedGoodsGrandTotalsAsync();

                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE);
                });

                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Items,
                    hasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE),
                    grandTotalBalanceQty = grandTotals.TotalBalanceQty,
                    grandTotalBalancePallet = grandTotals.TotalBalancePallet
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load paginated Finished Goods.");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load data"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Datatable()
        {
            var finishedGoods = await _finishedGoodService.GetFinishedGoodsAsync();
            ViewBag.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE);
            return View(finishedGoods); // Pass to DataTable.cshtml
        }
        [HttpGet]
        public async Task<IActionResult> Details(Guid id, bool showGrouped = false)
        {
            var summary = await _finishedGoodService.GetFinishedGoodSummaryByIdAsync(id);
            ViewBag.ShowGrouped = showGrouped;
            return View("Details", summary);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedReceives(Guid finishedGoodId, [FromBody] DataTablesRequest request)
        {
            try
            {
                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _finishedGoodService.GetPaginatedReceivesByFinishedGoodIdAsync(
                    finishedGoodId,
                    request.Start,
                    request.Length,
                    searchTerm,
                    sortColumn,
                    sortAscending
                );
                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE);
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
                _logger.LogError(ex, "Failed to load paginated receives for FinishedGood {FinishedGoodId}", finishedGoodId);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedPallets(Guid receiveId, [FromBody] DataTablesRequest request)
        {
            var result = await _finishedGoodService.GetPaginatedPalletsByReceiveIdAsync(
                receiveId,
                request.Start,
                request.Length
            );
            result.Items.ForEach(x =>
            {
                x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE);
            });
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                data = result.Items
            });
        }

        public async Task<IActionResult> Pallets(Guid ReceiveId)
        {
            
            return View("Pallets", ReceiveId);
        }
        public class PalletPhotoRequest
        {
            public string PalletId { get; set; } = default!;
        }

        [HttpGet]
        public async Task<IActionResult> ViewPhotoAttachment(Guid palletId)
        {
            var (success, message, urls) = await _finishedGoodService.GetPalletPhotoPathByIdAsync(palletId);

            if (!success || urls == null || !urls.Any())
            {
                return PartialView("_NoPhotoFoundPartial", message);
            }

            var model = new FG_PalletPhotoViewModel { PhotoUrl = urls };
            return PartialView("_PalletPhotoPartial", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedItems(Guid receiveId, bool grouped, [FromBody] DataTablesRequest request)
        {
            if (grouped)
            {
                var result = await _finishedGoodService.GetPaginatedGroupedItemsByReceive(receiveId, request.Start, request.Length);
                result.Items.ForEach(x =>
                {
                    x.HasEditAccess = _currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE);
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
                var result = await _finishedGoodService.GetPaginatedItemsByReceive(receiveId, request.Start, request.Length);

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
            if (grouped)
            {
                var groupedItems = await _finishedGoodService.GetGroupedItemsByReceive(receiveId);
                var model = new FinishedGoodItemsViewModel
                {
                    ReceiveId = receiveId,
                    ShowGrouped = true,
                    GroupedItems = groupedItems
                };
                return View(model);
            }
            else
            {
                var items = await _finishedGoodService.GetItemsByReceive(receiveId);
                var model = new FinishedGoodItemsViewModel
                {
                    ReceiveId = receiveId,
                    ShowGrouped = false,
                    Items = items
                };
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Unassigned()
        {
            var result = await _finishedGoodService.GetUnassignedPalletsAndSkusAsync();
            return View(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetPalletsBySku(Guid SkuId)
        {
            var pallets = await _finishedGoodService.GetPalletsBySkuAsync(SkuId);
            return Json(pallets);
        }
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AssignPalletsToSku([FromBody] AssignPalletsDto dto)
        {
            bool hasEditAccess = _currentUserService.HasPermission("FinishedGoods.Write");

            if (!hasEditAccess)
            {
                _logger.LogWarning("User {UserId} denied access to edit Finished Good - insufficient permissions",
                    _currentUserService.UserId);
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Access denied" }); 
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _finishedGoodService.AssignPalletsToSkuAsync(dto.SkuId, dto.PalletIds,dto.UnassignedReceiveIds);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetReceivesBySku(Guid skuId)
        {
            var receives = await _finishedGoodService.GetReceivesBySkuAsync(skuId);
            return Json(receives);
        }
        public async Task<IActionResult> Release(Guid finishedgoodId)
        {
            var dto = await _finishedGoodService.GetFinishedGoodReleaseDetailsAsync(finishedgoodId);
            return View(dto);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromBody] FinishedGoodReleaseSubmitDto dto)
        {
            _logger.LogInformation("Submitting Finished Good release for ID: {FinishedGoodId} by user {UserId}",
                dto.FinishedGoodId, _currentUserService.UserId);

            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // Check model validity
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Invalid model state: {Errors}", string.Join("; ", errors));

                if (isAjax)
                    return BadRequest(new { success = false, message = errors.FirstOrDefault() ?? "Invalid input", errors });

                var viewModelInvalid = await _finishedGoodService.GetFinishedGoodReleaseDetailsAsync(dto.FinishedGoodId);
                return View("Release", viewModelInvalid);
            }

            // Check if at least one pallet or item is selected for release
            bool hasPalletSelections = dto.PalletReleases != null && dto.PalletReleases.Any();
            bool hasItemSelections = dto.ItemIds != null && dto.ItemIds.Any();

            if (!hasPalletSelections && !hasItemSelections)
            {
                const string errorMsg = "Please select at least one pallet or item to release.";
                _logger.LogWarning(errorMsg);

                if (isAjax)
                    return BadRequest(new { success = false, message = errorMsg });

                ModelState.AddModelError("", errorMsg);
                _toastService.AddErrorToast(errorMsg);
                var viewModelEmpty = await _finishedGoodService.GetFinishedGoodReleaseDetailsAsync(dto.FinishedGoodId);
                return View("Release", viewModelEmpty);
            }

            try
            {
                var result = await _finishedGoodService.ReleaseFinishedGoodAsync(dto, _currentUserService.UserId);

                if (result.Success)
                {
                    if (isAjax)
                        return Json(new { success = true, message = "Release successful." });

                    TempData["SuccessMessage"] = "Finished Good released successfully.";
                    _toastService.AddSuccessToast("Finished Good released successfully.");
                    return RedirectToAction(nameof(Datatable));
                }

                if (isAjax)
                    return BadRequest(new { success = false, message = result.ErrorMessage ?? "Release failed." });

                ModelState.AddModelError("", result.ErrorMessage ?? "Release failed.");
                _toastService.AddErrorToast(result.ErrorMessage ?? "Release failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during release");

                if (isAjax)
                    return BadRequest(new { success = false, message = $"Exception during release: {ex.Message}" });

                ModelState.AddModelError("", $"Exception: {ex.Message}");
                _toastService.AddErrorToast($"Exception: {ex.Message}");
            }

            var viewModel = await _finishedGoodService.GetFinishedGoodReleaseDetailsAsync(dto.FinishedGoodId);
            return View("Release", viewModel);
        }
        public async Task<IActionResult> EditItem(Guid hu, Guid receiveId, bool grouped)
        {
            bool hasEditAccess = _currentUserService.HasPermission("FinishedGoods.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("FinishedGoods.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Finished Good - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }
            var item = await _finishedGoodService.GetItemById(hu);
            if (item == null) return NotFound();
            ViewBag.ReceiveId = receiveId;
            ViewBag.Grouped = grouped;
            return View(item);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(FG_ReceivePalletItemEditDto dto, Guid receiveId, bool grouped)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the validation errors.";
                ViewBag.ReceiveId = receiveId;
                ViewBag.Grouped = grouped;
                return View(dto);
            }

            var result = await _finishedGoodService.UpdateItemAsync(dto);

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
        //Finished Good Edit load
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            bool hasEditAccess = _currentUserService.HasPermission("FinishedGoods.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("FinishedGoods.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Finished Good - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }
            var dto = await _finishedGoodService.GetEditFinishedGoodDtoAsync(id);
            if (dto == null) return NotFound();

            return View("EditFinishedGood", dto);
        }

        // Finished Good Edit submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FinishedGoodEditDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _finishedGoodService.UpdateFinishedGoodAsync(dto, User.Identity?.Name ?? "system");

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Datatable"); // or any listing page
        }

        // Receive Edit load 
        [HttpGet]
        public async Task<IActionResult> EditReceive(Guid id, Guid finishedGoodId, bool showGrouped = false)
        {
            bool hasEditAccess = _currentUserService.HasPermission("FinishedGoods.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("FinishedGoods.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Finished Good - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }
            var dto = await _finishedGoodService.GetEditReceiveDtoAsync(id);
            if (dto == null) return NotFound();

            ViewBag.FinishedGoodId = finishedGoodId;
            ViewBag.ShowGrouped = showGrouped;
            return View("EditReceive", dto);
        }
        // Receive Edit submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReceive(FG_ReceiveEditDto dto, Guid finishedGoodId, bool showGrouped = false)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.FinishedGoodId = finishedGoodId;
                ViewBag.ShowGrouped = showGrouped;
                return View(dto);
            }

            var result = await _finishedGoodService.UpdateReceiveAsync(dto, User.Identity?.Name ?? "system");

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                ViewBag.FinishedGoodId = finishedGoodId;
                ViewBag.ShowGrouped = showGrouped;
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Details", new { id = finishedGoodId, showGrouped });
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
                var (fileContent, fileName) = await _finishedGoodService.GenerateFinishedGoodWeeklyExcelAsync(cutoff);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            if (ReportMode == "Monthly" && startMonth.HasValue && endMonth.HasValue)
            {
                var start = DateTime.SpecifyKind(startMonth.Value, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endMonth.Value, DateTimeKind.Utc);
                var (fileContent, fileName) = await _finishedGoodService.GenerateFinishedGoodExcelAsync(start, end);
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            TempData["Error"] = "Invalid input. Please select the appropriate date range.";
            return RedirectToAction("Datatable");
        }
        [HttpGet]
        public IActionResult CreateFinishedGood()
        {
            bool hasEditAccess = _currentUserService.HasPermission("FinishedGoods.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("FinishedGoods.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Finished Good - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FinishedGoodCreateWebDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);


            var result = await _finishedGoodService.CreateFinishedGoodAsync(dto);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(dto);
            }

            TempData["SuccessMessage"] = "Finished Good created successfully.";
            return RedirectToAction("DataTable");
        }

        [HttpGet]
        public async Task<IActionResult> EditPallet(Guid id, Guid receiveId)
        {
            bool hasEditAccess = _currentUserService.HasPermission("FinishedGoods.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("FinishedGoods.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit Finished Good - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            var (details, locationList) = await _finishedGoodService.GetReceivePalletForEditAsync(id);

            if (details == null)
                return NotFound();

            var editDto = new FG_ReceivePalletEditDto
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
        public async Task<IActionResult> EditPallet(FG_ReceivePalletEditDto model)
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

            var result = await _finishedGoodService.UpdatePalletAsync(model);
            return Json(result);
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
            bool hasEditAccess = _currentUserService.HasPermission("FinishedGoods.Write");

            if (!hasEditAccess)
            {
                _logger.LogWarning("User {UserId} tried to update pallet location without permission.", _currentUserService.UserId);
                return Forbid();
            }

            var result = await _finishedGoodService.UpdatePalletLocationAsync(
                dto.PalletId,
                dto.LocationId,
                _currentUserService.GetCurrentUsername);

            return result.Success ? Json(result) : BadRequest(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGroupFieldInline(Guid palletId, string fieldName, bool value)
        {
            if (!_currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE))
            {
                return Forbid();
            }

            var success = await _finishedGoodService.UpdatePalletGroupFieldAsync(
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGroupField(Guid finishedGoodId, string fieldName, bool value)
        {
            if (!_currentUserService.HasPermission(AppConsts.Permissions.FINISHED_GOODS_WRITE))
            {
                _logger.LogWarning("User {UserId} tried to update finished good group field without permission.", _currentUserService.UserId);
                return Forbid();
            }

            var result = await _finishedGoodService.UpdateFinishedGoodGroupFieldAsync(
                finishedGoodId,
                fieldName,
                value,
                _currentUserService.GetCurrentUsername);

            if (result.Success)
            {
                return Json(new { success = true, message = $"{fieldName} updated successfully" });
            }
            else
            {
                return Json(new { success = false, message = result.Message ?? "Failed to update group field" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ProcessPendingFinishedGoodReleases(CancellationToken cancellation = default)
        {
            try
            {
                // Create a session key that's unique for today
                string sessionKey = $"ProcessedFGReleases_{DateTime.UtcNow:yyyyMMdd}";

                // Check if we've already processed releases today
                if (HttpContext.Session.GetString(sessionKey) == null)
                {
                    await _finishedGoodService.ProcessFinishedGoodReleases(DateTime.UtcNow, cancellation);

                    // Set the session flag to prevent processing again today
                    HttpContext.Session.SetString(sessionKey, "Processed");

                    return Json(new { success = true, message = "Pending finished good releases processed successfully" });
                }

                return Json(new { success = true, message = "Finished good releases already processed today" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending finished good releases");
                return Json(new { success = false, message = "Error processing pending finished good releases", error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Releases(Guid id)
        {
            var finishedGood = await _finishedGoodService.GetFinishedGoodDetailsByIdAsync(id);
            if (finishedGood == null)
                return NotFound();

            return View("Releases", finishedGood);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedReleases(Guid finishedGoodId, [FromBody] DataTablesRequest request)
        {
            try
            {
                var result = await _finishedGoodService.GetPaginatedReleasesByFinishedGoodIdAsync(
                    finishedGoodId,
                    request.Start,
                    request.Length,
                    request.Search?.Value,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
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
                _logger.LogError(ex, "Error loading releases for FinishedGoodId={FinishedGoodId}", finishedGoodId);
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
        public async Task<IActionResult> ReleaseDetails(Guid id, Guid finishedGoodId)
        {
            var releaseDetails = await _finishedGoodService.GetReleaseDetailsByIdAsync(id);
            if (releaseDetails == null)
                return NotFound();

            ViewBag.FinishedGoodId = finishedGoodId;
            return View("ReleaseDetails", releaseDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaginatedReleaseDetails(Guid releaseId, [FromBody] DataTablesRequest request)
        {
            try
            {
                var result = await _finishedGoodService.GetPaginatedReleaseDetailsAsync(
                    releaseId,
                    request.Start,
                    request.Length,
                    request.Search?.Value,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
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
                _logger.LogError(ex, "Error loading release details for FinishedGood ReleaseId={ReleaseId}", releaseId);
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
            if (!_currentUserService.HasPermission("FinishedGood.Read"))
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
                if (!_currentUserService.HasPermission("FinishedGood.Read"))
                {
                    return Forbid();
                }

                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _finishedGoodService.GetPaginatedJobReleasesAsync(
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
        [Route("FinishedGood/JobReleaseDetails/{jobId:guid}")]
        public async Task<IActionResult> JobReleaseDetails(Guid jobId)
        {
            _logger.LogInformation("Accessing Job Release Details for JobId {JobId} by user {UserId}",
                jobId, _currentUserService.UserId);

            // Check permissions
            if (!_currentUserService.HasPermission("FinishedGood.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to Job Release Details - insufficient permissions",
                    _currentUserService.UserId);
                return Forbid();
            }

            var jobReleaseDetails = await _finishedGoodService.GetJobReleaseDetailsByJobIdAsync(jobId);
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
                if (!_currentUserService.HasPermission("FinishedGood.Read"))
                {
                    return Forbid();
                }

                string searchTerm = request.Search?.Value;
                int sortColumn = request.Order?.FirstOrDefault()?.Column ?? 0;
                bool sortAscending = request.Order?.FirstOrDefault()?.Dir == "asc";

                var result = await _finishedGoodService.GetPaginatedJobReleaseIndividualReleasesAsync(
                    jobId,
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
        public async Task<IActionResult> ExportJobReleaseExcel(Guid jobId)
        {
            try
            {
                _logger.LogInformation("Exporting job release Excel for JobId {JobId} by user {UserId}",
                    jobId, _currentUserService.UserId);

                // Check permissions
                if (!_currentUserService.HasPermission("FinishedGood.Read"))
                {
                    _logger.LogWarning("User {UserId} denied access to export job release - insufficient permissions",
                        _currentUserService.UserId);
                    return Forbid();
                }

                var (fileContent, fileName) = await _finishedGoodService.ExportJobReleaseToExcelAsync(jobId);

                _logger.LogInformation("Job release Excel export completed for JobId: {JobId}, File: {FileName}",
                    jobId, fileName);

                return File(fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Job release not found for export: JobId={JobId}", jobId);
                return NotFound("Job release not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting job release Excel for JobId={JobId}", jobId);
                return BadRequest("Failed to export job release data");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckFinishedGoodReleaseConflicts([FromBody] FinishedGoodConflictCheckRequest request)
        {
            try
            {
                // Check permissions
                if (!_currentUserService.HasPermission("FinishedGood.Read"))
                {
                    return Forbid();
                }

                var conflicts = await _finishedGoodService.GetFinishedGoodReleaseConflictsAsync(request.FinishedGoodId);

                return Json(new
                {
                    success = true,
                    conflicts = conflicts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking finished good release conflicts for FinishedGoodId {FinishedGoodId}",
                    request.FinishedGoodId);
                return Json(new
                {
                    success = false,
                    message = "Failed to check release conflicts"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableFinishedGoodsForJobRelease()
        {
            try
            {
                // Check permissions
                if (!_currentUserService.HasPermission("FinishedGood.Read"))
                {
                    return Forbid();
                }

                var finishedGoods = await _finishedGoodService.GetAvailableFinishedGoodsForJobReleaseAsync();

                return Json(new
                {
                    success = true,
                    data = finishedGoods
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available finished goods for job release");
                return Json(new
                {
                    success = false,
                    message = "Failed to load available finished goods"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetFinishedGoodInventoryForJobRelease([FromBody] List<Guid> finishedGoodIds)
        {
            try
            {
                // Check permissions
                if (!_currentUserService.HasPermission("FinishedGood.Read"))
                {
                    return Forbid();
                }

                if (finishedGoodIds == null || !finishedGoodIds.Any())
                {
                    return BadRequest(new { success = false, message = "No finished good IDs provided" });
                }

                var inventory = await _finishedGoodService.GetFinishedGoodInventoryForJobReleaseAsync(finishedGoodIds);

                return Json(new
                {
                    success = true,
                    data = inventory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching finished good inventory for job release");
                return Json(new
                {
                    success = false,
                    message = "Failed to load finished good inventory"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetBatchFinishedGoodReleaseConflicts([FromBody] List<Guid> finishedGoodIds)
        {
            try
            {
                // Check permissions
                if (!_currentUserService.HasPermission("FinishedGood.Read"))
                {
                    return Forbid();
                }

                if (finishedGoodIds == null || !finishedGoodIds.Any())
                {
                    return BadRequest(new { success = false, message = "No finished good IDs provided" });
                }

                var conflicts = await _finishedGoodService.GetBatchFinishedGoodReleaseConflictsAsync(finishedGoodIds);

                return Json(new
                {
                    success = true,
                    data = conflicts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking batch finished good release conflicts");
                return Json(new
                {
                    success = false,
                    message = "Failed to check release conflicts"
                });
            }
        }

        #endregion
    }
}
