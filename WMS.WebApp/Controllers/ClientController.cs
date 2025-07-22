using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs.Clients;
using WMS.Domain.Interfaces;
using WMS.WebApp.Extensions;
using WMS.WebApp.Models.DataTables;
using WMS.WebApp.Models.Clients;
using WMS.Domain.DTOs;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private readonly IClientService _clientService;
        private readonly IWarehouseService _warehouseService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ToastService _toastService;
        private readonly ILogger<ClientController> _logger;

        public ClientController(
            IClientService clientService,
            IWarehouseService warehouseService,
            ICurrentUserService currentUserService,
            ToastService toastService,
            IMapper mapper,
            ILogger<ClientController> logger)
        {
            _clientService = clientService;
            _warehouseService = warehouseService;
            _currentUserService = currentUserService;
            _toastService = toastService;
            _mapper = mapper;
            _logger = logger;
        }
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.CLIENT_READ}")]
        public IActionResult Index()
        {
            _logger.LogInformation("Client index page accessed by user {UserId}", _currentUserService.UserId);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetClients(DataTablesRequest request)
        {
            _logger.LogDebug("Getting clients: SearchTerm={SearchTerm}, Start={Start}, Length={Length}",
                request.Search?.Value, request.Start, request.Length);

            try
            {
                var result = await _clientService.GetPaginatedClients(
                    request.Search?.Value,
                    request.Start,
                    request.Length,
                    request.Order?.FirstOrDefault()?.Column ?? 0,
                    request.Order?.FirstOrDefault()?.Dir == "asc"
                );

                _logger.LogDebug("Retrieved {Count} clients out of {Total}", result.Items.Count, result.TotalCount);

                result.Items.ForEach(x =>
                {
                    x.HasWriteAccess = _currentUserService.HasPermission(AppConsts.Permissions.CLIENT_WRITE);
                    x.HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.CLIENT_DELETE);
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
                _logger.LogError(ex, "Error retrieving clients");
                return Json(new
                {
                    draw = request.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load clients"
                });
            }
        }

        /// <summary>
        /// Display the create Client form
        /// </summary>
        [Authorize(Policy = $"Permission_{AppConsts.Permissions.CLIENT_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Creating new client - accessed by user {UserId}", _currentUserService.UserId);

            var viewModel = new ClientPageViewModel();
            viewModel.HasEditAccess = _currentUserService.HasPermission("Client.Write");

            // Load warehouses for dropdown
            await LoadWarehousesAsync(viewModel);

            return View("Detail", viewModel);
        }

        /// <summary>
        /// Process the create Client form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientViewModel viewModel)
        {
            _logger.LogInformation("Creating client: {ClientName} by user {UserId}",
                viewModel.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to DTO
                    var clientCreateDto = new ClientCreateDto
                    {
                        Name = viewModel.Name,
                        Code = viewModel.Code,
                        ContactPerson = viewModel.ContactPerson,
                        ContactEmail = viewModel.ContactEmail,
                        ContactPhone = viewModel.ContactPhone,
                        BillingAddress = viewModel.BillingAddress,
                        ShippingAddress = viewModel.ShippingAddress,
                        IsActive = viewModel.IsActive,
                        WarehouseId = viewModel.WarehouseId,
                        // Configuration
                        RequiresQualityCheck = viewModel.RequiresQualityCheck,
                        AutoGenerateReceivingLabels = viewModel.AutoGenerateReceivingLabels,
                        AutoGenerateShippingLabels = viewModel.AutoGenerateShippingLabels,
                        HandlingFeePercentage = viewModel.HandlingFeePercentage,
                        StorageFeePerCubicMeter = viewModel.StorageFeePerCubicMeter,
                        DefaultLeadTimeDays = viewModel.DefaultLeadTimeDays,
                        LowStockThreshold = viewModel.LowStockThreshold,
                        SendLowStockAlerts = viewModel.SendLowStockAlerts,
                        AllowPartialShipments = viewModel.AllowPartialShipments,
                        RequiresAppointmentForReceiving = viewModel.RequiresAppointmentForReceiving
                    };

                    // Create Client
                    var createdClient = await _clientService.CreateClientAsync(clientCreateDto);

                    _logger.LogInformation("Client created successfully: {ClientId} - {ClientName} by user {UserId}",
                        createdClient.Id, createdClient.Name, _currentUserService.UserId);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Client created successfully!");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating client: {ClientName} by user {UserId}",
                        viewModel.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error creating Client: {ex.Message}");

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
            else
            {
                _logger.LogWarning("Invalid model state when creating client by user {UserId}", _currentUserService.UserId);
            }

            var pageViewModel = new ClientPageViewModel
            {
                Client = viewModel,
                HasEditAccess = _currentUserService.HasPermission("Client.Write")
            };
            await LoadWarehousesAsync(pageViewModel);

            return View("Detail", pageViewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.CLIENT_READ}")]
        [HttpGet]
        public async Task<IActionResult> View(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("View client accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Viewing client {ClientId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Client.Write");

            // Check if user has read permission
            if (!hasEditAccess && !_currentUserService.HasPermission("Client.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to view client {ClientId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var client = await _clientService.GetClientByIdAsync(id.Value);

                if (client == null)
                {
                    _logger.LogWarning("Client {ClientId} not found when accessed by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new ClientPageViewModel
                {
                    Client = new ClientViewModel
                    {
                        Id = client.Id,
                        Name = client.Name,
                        Code = client.Code,
                        ContactPerson = client.ContactPerson,
                        ContactEmail = client.ContactEmail,
                        ContactPhone = client.ContactPhone,
                        BillingAddress = client.BillingAddress,
                        ShippingAddress = client.ShippingAddress,
                        IsActive = client.IsActive,
                        WarehouseId = client.WarehouseId,
                        WarehouseName = client.WarehouseName,
                        // Configuration
                        RequiresQualityCheck = client.Configuration?.RequiresQualityCheck ?? false,
                        AutoGenerateReceivingLabels = client.Configuration?.AutoGenerateReceivingLabels ?? false,
                        AutoGenerateShippingLabels = client.Configuration?.AutoGenerateShippingLabels ?? false,
                        HandlingFeePercentage = client.Configuration?.HandlingFeePercentage ?? 0,
                        StorageFeePerCubicMeter = client.Configuration?.StorageFeePerCubicMeter ?? 0,
                        DefaultLeadTimeDays = client.Configuration?.DefaultLeadTimeDays ?? 0,
                        LowStockThreshold = client.Configuration?.LowStockThreshold ?? 0,
                        SendLowStockAlerts = client.Configuration?.SendLowStockAlerts ?? false,
                        AllowPartialShipments = client.Configuration?.AllowPartialShipments ?? false,
                        RequiresAppointmentForReceiving = client.Configuration?.RequiresAppointmentForReceiving ?? false
                    },
                    HasEditAccess = false, // View mode
                    IsEdit = true
                };

                await LoadWarehousesAsync(viewModel);

                _logger.LogDebug("Successfully loaded client {ClientId} for viewing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client {ClientId} for user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load client details");
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.CLIENT_WRITE}")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!id.HasValue)
            {
                _logger.LogWarning("Edit client accessed without ID by user {UserId}", _currentUserService.UserId);
                return NotFound();
            }

            _logger.LogInformation("Editing client {ClientId} by user {UserId}", id.Value, _currentUserService.UserId);

            bool hasEditAccess = _currentUserService.HasPermission("Client.Write");

            if (!hasEditAccess && !_currentUserService.HasPermission("Client.Read"))
            {
                _logger.LogWarning("User {UserId} denied access to edit client {ClientId} - insufficient permissions",
                    _currentUserService.UserId, id.Value);
                return Forbid();
            }

            try
            {
                var client = await _clientService.GetClientByIdAsync(id.Value);

                if (client == null)
                {
                    _logger.LogWarning("Client {ClientId} not found when accessed for editing by user {UserId}",
                        id.Value, _currentUserService.UserId);
                    return NotFound();
                }

                var viewModel = new ClientPageViewModel
                {
                    Client = new ClientViewModel
                    {
                        Id = client.Id,
                        Name = client.Name,
                        Code = client.Code,
                        ContactPerson = client.ContactPerson,
                        ContactEmail = client.ContactEmail,
                        ContactPhone = client.ContactPhone,
                        BillingAddress = client.BillingAddress,
                        ShippingAddress = client.ShippingAddress,
                        IsActive = client.IsActive,
                        WarehouseId = client.WarehouseId,
                        WarehouseName = client.WarehouseName,
                        // Configuration
                        RequiresQualityCheck = client.Configuration?.RequiresQualityCheck ?? false,
                        AutoGenerateReceivingLabels = client.Configuration?.AutoGenerateReceivingLabels ?? false,
                        AutoGenerateShippingLabels = client.Configuration?.AutoGenerateShippingLabels ?? false,
                        HandlingFeePercentage = client.Configuration?.HandlingFeePercentage ?? 0,
                        StorageFeePerCubicMeter = client.Configuration?.StorageFeePerCubicMeter ?? 0,
                        DefaultLeadTimeDays = client.Configuration?.DefaultLeadTimeDays ?? 0,
                        LowStockThreshold = client.Configuration?.LowStockThreshold ?? 0,
                        SendLowStockAlerts = client.Configuration?.SendLowStockAlerts ?? false,
                        AllowPartialShipments = client.Configuration?.AllowPartialShipments ?? false,
                        RequiresAppointmentForReceiving = client.Configuration?.RequiresAppointmentForReceiving ?? false
                    },
                    HasEditAccess = hasEditAccess,
                    IsEdit = true
                };

                await LoadWarehousesAsync(viewModel);

                return View("Detail", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client {ClientId} for editing by user {UserId}",
                    id.Value, _currentUserService.UserId);

                _toastService.AddErrorToast("Failed to load client details");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientViewModel model)
        {
            _logger.LogInformation("Updating client {ClientId} - {ClientName} by user {UserId}",
                model.Id, model.Name, _currentUserService.UserId);

            if (ModelState.IsValid)
            {
                try
                {
                    bool hasEditAccess = _currentUserService.HasPermission("Client.Write");

                    if (!hasEditAccess)
                    {
                        _logger.LogWarning("User {UserId} denied access to update client {ClientId} - insufficient permissions",
                            _currentUserService.UserId, model.Id);
                        return Forbid();
                    }

                    var clientUpdateDto = new ClientUpdateDto
                    {
                        Name = model.Name,
                        Code = model.Code,
                        ContactPerson = model.ContactPerson,
                        ContactEmail = model.ContactEmail,
                        ContactPhone = model.ContactPhone,
                        BillingAddress = model.BillingAddress,
                        ShippingAddress = model.ShippingAddress,
                        IsActive = model.IsActive,
                        // Configuration
                        RequiresQualityCheck = model.RequiresQualityCheck,
                        AutoGenerateReceivingLabels = model.AutoGenerateReceivingLabels,
                        AutoGenerateShippingLabels = model.AutoGenerateShippingLabels,
                        HandlingFeePercentage = model.HandlingFeePercentage,
                        StorageFeePerCubicMeter = model.StorageFeePerCubicMeter,
                        DefaultLeadTimeDays = model.DefaultLeadTimeDays,
                        LowStockThreshold = model.LowStockThreshold,
                        SendLowStockAlerts = model.SendLowStockAlerts,
                        AllowPartialShipments = model.AllowPartialShipments,
                        RequiresAppointmentForReceiving = model.RequiresAppointmentForReceiving
                    };

                    await _clientService.UpdateClientAsync(model.Id, clientUpdateDto);

                    _logger.LogInformation("Client updated successfully: {ClientId} - {ClientName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true });
                    }

                    _toastService.AddSuccessToast("Client updated successfully!");
                    return RedirectToAction("View", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating client {ClientId} - {ClientName} by user {UserId}",
                        model.Id, model.Name, _currentUserService.UserId);

                    ModelState.AddModelError("", $"Error updating Client: {ex.Message}");

                    if (Request.IsAjaxRequest())
                    {
                        return BadRequest(new { success = false, message = ex.Message });
                    }
                    _toastService.AddErrorToast($"{ex.Message}");
                }
            }
            else if (Request.IsAjaxRequest())
            {
                _logger.LogWarning("Invalid model state when updating client {ClientId} by user {UserId}",
                    model.Id, _currentUserService.UserId);
                return BadRequest(new { success = false, errors = ModelState });
            }

            var viewModel = new ClientPageViewModel
            {
                Client = model,
                HasEditAccess = _currentUserService.HasPermission("Client.Write"),
                IsEdit = true
            };
            await LoadWarehousesAsync(viewModel);

            return View("Detail", viewModel);
        }

        [Authorize(Policy = $"Permission_{AppConsts.Permissions.CLIENT_DELETE}")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid clientId)
        {
            _logger.LogInformation("Deleting client {ClientId} by user {UserId}", clientId, _currentUserService.UserId);

            try
            {
                await _clientService.DeleteClientAsync(clientId);

                _logger.LogInformation("Client deleted successfully: {ClientId} by user {UserId}",
                    clientId, _currentUserService.UserId);

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Client deleted successfully" });
                }

                _toastService.AddSuccessToast("Client deleted successfully");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client {ClientId} by user {UserId}",
                    clientId, _currentUserService.UserId);

                var errorMessage = $"Failed to delete client: {ex.Message}";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = errorMessage });
                }

                _toastService.AddErrorToast(errorMessage);
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task LoadWarehousesAsync(ClientPageViewModel viewModel)
        {
            try
            {
                bool isAdmin = _currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN);

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
                _logger.LogError(ex, "Error loading warehouses for client form");
                viewModel.Warehouses = new List<WarehouseDropdownItem>();
            }
        }
    }
}