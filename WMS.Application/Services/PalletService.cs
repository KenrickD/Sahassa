using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;

namespace WMS.Application.Services
{
    public class PalletService : IPalletService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILocationService _locationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PalletService> _logger;
        public PalletService(
            AppDbContext dbContext,
            IMapper mapper,
            ILocationService locationService,
            ICurrentUserService currentUserService,
            ILogger<PalletService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _locationService = locationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }
        public async Task<ApiResponseDto<string>> UpdatePalletLocation(UpdatePalletLocationDto dto, string username, Guid warehouseId)
        {
            _logger.LogInformation("Updating pallet location. Username: {Username}, WarehouseId: {WarehouseId}, Code: {Code}, LocationId: {LocationId}", username, warehouseId, dto.Code, dto.LocationId);

            var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == warehouseId && !w.IsDeleted);
            if (!warehouseExists)
            {
                _logger.LogWarning("Warehouse not found: {WarehouseId}", warehouseId);
                return ApiResponseDto<string>.ErrorResult($"Warehouse '{warehouseId}' not found.", new List<string> { "Invalid warehouse ID." });
            }

            if (dto.LocationId == null || dto.LocationId == "")
            {
                _logger.LogWarning("Location ID is missing");
                return ApiResponseDto<string>.ErrorResult("Location ID is required.", new List<string> { "Location is mandatory." });
            }

            var location = await _dbContext.Locations.FirstOrDefaultAsync(l => l.Barcode == dto.LocationId && !l.IsDeleted);
            if (location == null)
            {
                _logger.LogWarning("Location not found: {LocationId}", dto.LocationId);
                return ApiResponseDto<string>.ErrorResult($"Location '{dto.LocationId}' not found.", new List<string> { "Invalid location ID." });
            }

            var totalPalletsInLocation = await _locationService.CheckLocationAvailabilityByBarcodeAsync(location.Barcode);
            if (!totalPalletsInLocation)
            {
                _logger.LogWarning("Location full: {LocationCode}", location.Code);
                return ApiResponseDto<string>.ErrorResult($"Location '{location.Code}' is full.", new List<string> { "Maximum capacity reached for this location." });
            }

            var code = dto.Code?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Code is empty");
                return ApiResponseDto<string>.ErrorResult("Code is required.", new List<string> { "Code cannot be empty." });
            }

            var fgPallet = await _dbContext.GIV_FG_ReceivePallets.Include(p => p.Location).FirstOrDefaultAsync(p => p.PalletCode == code && !p.IsDeleted);
            if (fgPallet != null)
            {
                //Arif Tan 2025-07-10, comment code for user able reassign the location
                //if (fgPallet.LocationId != null)
                //{
                //    var locCode = fgPallet.Location?.Code ?? "assigned";
                //    _logger.LogInformation("FG pallet already assigned: {Code} -> {LocationCode}", code, locCode);
                //    return ApiResponseDto<string>.SuccessResult($"Finished Goods pallet already has a location '{locCode}'.", "Pallet already assigned.");
                //}

                fgPallet.LocationId = location.Id;
                fgPallet.StoredBy = dto.StoredBy;
                fgPallet.ModifiedBy = username;
                fgPallet.ModifiedAt = DateTime.UtcNow;
                location.IsEmpty = false;

                await _locationService.MarkLocationAsOccupiedAsync(location.Id);
                //await MarkLocationAsFull(location.Id, location.Barcode);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("FG pallet location updated successfully: {Code} -> {LocationCode}", code, location.Code);
                return ApiResponseDto<string>.SuccessResult("Finished Goods Pallet location updated successfully.", "Success");
            }

            var rmPallet = await _dbContext.GIV_RM_ReceivePallets.Include(p => p.Location).FirstOrDefaultAsync(p => p.PalletCode == code && !p.IsDeleted);
            if (rmPallet != null)
            {
                //Arif Tan 2025-07-10, comment code for user able reassign the location
                //if (rmPallet.LocationId != null)
                //{
                //    var locCode = rmPallet.Location?.Code ?? "assigned";
                //    _logger.LogInformation("RM pallet already assigned: {Code} -> {LocationCode}", code, locCode);
                //    return ApiResponseDto<string>.SuccessResult($"Raw Material pallet already has a location '{locCode}'.", "Pallet already assigned.");
                //}

                rmPallet.LocationId = location.Id;
                rmPallet.StoredBy = dto.StoredBy;
                rmPallet.ModifiedBy = username;
                rmPallet.ModifiedAt = DateTime.UtcNow;
                location.IsEmpty = false;

                await _locationService.MarkLocationAsOccupiedAsync(location.Id);
                //await MarkLocationAsFull(location.Id, location.Barcode);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("RM pallet location updated successfully: {Code} -> {LocationCode}", code, location.Code);
                return ApiResponseDto<string>.SuccessResult("Raw Material Pallet location updated successfully.", "Success");
            }

            var fgItem = await _dbContext.GIV_FG_ReceivePalletItems
                .Include(i => i.GIV_FG_ReceivePallet)
                    .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(i => i.ItemCode == code && !i.IsDeleted);

            if (fgItem != null)
            {
                var pallet = fgItem.GIV_FG_ReceivePallet;

                //Arif Tan 2025-07-10, comment code for user able reassign the location
                //if (pallet.LocationId != null)
                //{
                //    var locCode = pallet.Location?.Code ?? "assigned";
                //    _logger.LogInformation("FG item pallet already assigned: {Code} -> {LocationCode}", code, locCode);
                //    return ApiResponseDto<string>.ErrorResult($"Finished Goods pallet already has a location '{locCode}'.", new List<string> { "Pallet already assigned." });
                //}

                pallet.LocationId = location.Id;
                pallet.StoredBy = dto.StoredBy;
                pallet.ModifiedBy = username;
                pallet.ModifiedAt = DateTime.UtcNow;
                location.IsEmpty = false;

                await _locationService.MarkLocationAsOccupiedAsync(location.Id);
                //await MarkLocationAsFull(location.Id, location.Barcode);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("FG item (via ItemCode) location updated successfully: {Code} -> {LocationCode}", code, location.Code);
                return ApiResponseDto<string>.SuccessResult("Finished Goods Pallet (by ItemCode) location updated successfully.", "Success");
            }

            var rmItem = await _dbContext.GIV_RM_ReceivePalletItems
                .Include(i => i.GIV_RM_ReceivePallet)
                    .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(i => i.ItemCode == code && !i.IsDeleted);

            if (rmItem != null)
            {
                var pallet = rmItem.GIV_RM_ReceivePallet;

                //Arif Tan 2025-07-10, comment code for user able reassign the location
                //if (pallet.LocationId != null)
                //{
                //    var locCode = pallet.Location?.Code ?? "assigned";
                //    _logger.LogInformation("RM item pallet already assigned: {Code} -> {LocationCode}", code, locCode);
                //    return ApiResponseDto<string>.ErrorResult($"Raw Material pallet already has a location '{locCode}'.", new List<string> { "Pallet already assigned." });
                //}

                pallet.LocationId = location.Id;
                pallet.StoredBy = dto.StoredBy;
                pallet.ModifiedBy = username;
                pallet.ModifiedAt = DateTime.UtcNow;
                location.IsEmpty = false;

                await _locationService.MarkLocationAsOccupiedAsync(location.Id);
                //await MarkLocationAsFull(location.Id, location.Barcode);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("RM item (via ItemCode) location updated successfully: {Code} -> {LocationCode}", code, location.Code);
                return ApiResponseDto<string>.SuccessResult("Raw Material Pallet (by ItemCode) location updated successfully.", "Success");
            }

            _logger.LogWarning("Code not found for pallet/item: {Code}", code);
            return ApiResponseDto<string>.ErrorResult($"No pallet or item found with code '{code}'.", new List<string> { "Invalid Code." });
        }



        public async Task<ApiResponseDto<string>> ReleasePalletLocation(ReleasePalletLocationDto dto, string username, Guid warehouseId)
        {
            _logger.LogInformation("Starting ReleasePalletLocation for Code: {Code}, Location: {LocationId}, Warehouse: {WarehouseId}", dto.Code, dto.LocationId, warehouseId);

            // 1. Validate warehouse
            var warehouseExists = await _dbContext.Warehouses
                .AnyAsync(w => w.Id == warehouseId && !w.IsDeleted);

            if (!warehouseExists)
            {
                _logger.LogWarning("Warehouse not found: {WarehouseId}", warehouseId);
                throw new KeyNotFoundException($"Warehouse with ID {warehouseId} not found.");
            }

            // 2. Validate location
            var location = await _dbContext.Locations
                .FirstOrDefaultAsync(l => l.Barcode == dto.LocationId && l.WarehouseId == warehouseId && !l.IsDeleted);

            if (location == null)
            {
                _logger.LogWarning("Location not found: {LocationId} in Warehouse: {WarehouseId}", dto.LocationId, warehouseId);
                return ApiResponseDto<string>.ErrorResult(
                    $"Location '{dto.LocationId}' not found in warehouse '{warehouseId}.",
                    new List<string> { "Invalid location code." }
                );
            }

            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                _logger.LogWarning("Empty code provided for release at Location: {LocationId}", dto.LocationId);
                return ApiResponseDto<string>.ErrorResult(
                    "Code is required.",
                    new List<string> { "No pallet or item code specified." }
                );
            }

            string inputCode = dto.Code.Trim();
            GIV_RM_ReceivePallet? rmPallet = null;
            GIV_FG_ReceivePallet? fgPallet = null;

            // 3. Try as PalletCode (FG first)
            fgPallet = await _dbContext.GIV_FG_ReceivePallets
                .Include(p => p.Location)
                .Include(p => p.FG_ReceivePalletItems)
                .FirstOrDefaultAsync(p =>
                    p.PalletCode == inputCode &&
                    p.LocationId == location.Id &&
                    !p.IsDeleted);

            if (fgPallet == null)
            {
                rmPallet = await _dbContext.GIV_RM_ReceivePallets
                    .Include(p => p.Location)
                    .Include(p => p.RM_ReceivePalletItems)
                    .FirstOrDefaultAsync(p =>
                        p.PalletCode == inputCode &&
                        p.LocationId == location.Id &&
                        !p.IsDeleted);
            }

            // 4. If not found as PalletCode, try as ItemCode (FG first)
            if (fgPallet == null && rmPallet == null)
            {
                var fgItem = await _dbContext.GIV_FG_ReceivePalletItems
                    .Include(i => i.GIV_FG_ReceivePallet)
                        .ThenInclude(p => p.Location)
                    .Include(i => i.GIV_FG_ReceivePallet)
                        .ThenInclude(p => p.FG_ReceivePalletItems)
                    .FirstOrDefaultAsync(i =>
                        i.ItemCode == inputCode &&
                        !i.IsDeleted &&
                        i.GIV_FG_ReceivePallet.LocationId == location.Id &&
                        !i.GIV_FG_ReceivePallet.IsDeleted);

                fgPallet = fgItem?.GIV_FG_ReceivePallet;

                if (fgPallet == null)
                {
                    var rmItem = await _dbContext.GIV_RM_ReceivePalletItems
                        .Include(i => i.GIV_RM_ReceivePallet)
                            .ThenInclude(p => p.Location)
                        .Include(i => i.GIV_RM_ReceivePallet)
                            .ThenInclude(p => p.RM_ReceivePalletItems)
                        .FirstOrDefaultAsync(i =>
                            i.ItemCode == inputCode &&
                            !i.IsDeleted &&
                            i.GIV_RM_ReceivePallet.LocationId == location.Id &&
                            !i.GIV_RM_ReceivePallet.IsDeleted);

                    rmPallet = rmItem?.GIV_RM_ReceivePallet;
                }
            }

            string? palletCode = fgPallet?.PalletCode ?? rmPallet?.PalletCode;

            // 5. No match
            if (fgPallet == null && rmPallet == null)
            {
                _logger.LogWarning("No matching pallet found with code '{Code}' at location '{LocationId}'", dto.Code, dto.LocationId);
                return ApiResponseDto<string>.ErrorResult(
                    $"No matching pallet found with code '{dto.Code}' at location '{dto.LocationId}.",
                    new List<string> { "Pallet or item not associated with the specified location." }
                );
            }

            // 6. FG: direct release
            if (fgPallet != null)
            {
                fgPallet.Location!.IsEmpty = true;
                fgPallet.LocationId = null;
                //fgPallet.IsReleased = true;

                // Release all items in the FG pallet
                //foreach (var item in fgPallet.FG_ReceivePalletItems)
                //{
                //    item.IsReleased = true;
                //}

                _logger.LogInformation("Released Finished Goods pallet '{PalletCode}' from location '{LocationId}'", palletCode, dto.LocationId);
            }
            // 7. RM: only if all items are released
            else if (rmPallet != null)
            {
                //rmPallet.IsReleased = true;
                rmPallet.Location!.IsEmpty = true;
                rmPallet.LocationId = null;

                // Release all items in the RM pallet
                //foreach (var item in rmPallet.RM_ReceivePalletItems)
                //{
                //    item.IsReleased = true;
                //}

                _logger.LogInformation("Released Raw Material pallet '{PalletCode}' from location '{LocationId}'", palletCode, dto.LocationId);
            }

            await _locationService.MarkLocationAsEmptyAsync(location.Id);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Location '{LocationCode}' successfully cleared from pallet '{PalletCode}'", location.Code, palletCode);

            return ApiResponseDto<string>.SuccessResult(
                palletCode!,
                $"Location '{location.Code}' successfully cleared from pallet '{palletCode}'."
            );
        }

        public async Task<ApiResponseDto<BulkUpdatePalletLocationResultDto>> BulkUpdatePalletLocationAsync(BulkUpdatePalletLocationDto dto, string username, Guid warehouseId)
        {
            _logger.LogInformation("Starting bulk pallet location update. Username: {Username}, WarehouseId: {WarehouseId}, PalletCount: {PalletCount}, LocationId: {LocationId}",
                username, warehouseId, dto.PalletCodes?.Count ?? 0, dto.LocationBarcode);

            // Input validation
            var validationResult = ValidateBulkUpdateRequest(dto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Bulk update validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                return ApiResponseDto<BulkUpdatePalletLocationResultDto>.ErrorResult(
                    "Validation failed", validationResult.Errors);
            }

            // Warehouse validation
            var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == warehouseId && !w.IsDeleted);
            if (!warehouseExists)
            {
                _logger.LogWarning("Warehouse not found: {WarehouseId}", warehouseId);
                return ApiResponseDto<BulkUpdatePalletLocationResultDto>.ErrorResult(
                    $"Warehouse '{warehouseId}' not found.",
                    new List<string> { "Invalid warehouse ID." });
            }

            // Location validation
            var location = await _dbContext.Locations.FirstOrDefaultAsync(l => l.Barcode == dto.LocationBarcode && !l.IsDeleted);
            if (location == null)
            {
                _logger.LogWarning("Location not found: {LocationBarcode}", dto.LocationBarcode);
                return ApiResponseDto<BulkUpdatePalletLocationResultDto>.ErrorResult(
                    $"Location '{dto.LocationBarcode}' not found.",
                    new List<string> { "Invalid location barcode." });
            }

            var result = new BulkUpdatePalletLocationResultDto
            {
                TotalRequested = dto.PalletCodes.Count,
                SuccessfulUpdates = new List<PalletUpdateResult>(),
                FailedUpdates = new List<PalletUpdateResult>(),
                LocationCode = location.Code,
                LocationBarcode = location.Barcode
            };

            // Check location capacity before processing
            var locationCapacityResult = await ValidateLocationCapacity(location, dto.PalletCodes.Count);
            if (!locationCapacityResult.CanAccommodate)
            {
                _logger.LogWarning("Location capacity insufficient: {LocationCode}, Required: {Required}, Available: {Available}",
                    location.Code, dto.PalletCodes.Count, locationCapacityResult.AvailableCapacity);

                return ApiResponseDto<BulkUpdatePalletLocationResultDto>.ErrorResult(
                    $"Location '{location.Code}' has insufficient capacity. Required: {dto.PalletCodes.Count}, Available: {locationCapacityResult.AvailableCapacity}",
                    new List<string> { "Insufficient location capacity for bulk update." });
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var processedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var locationUpdated = false;

                foreach (var code in dto.PalletCodes)
                {
                    var trimmedCode = code?.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedCode))
                    {
                        result.FailedUpdates.Add(new PalletUpdateResult
                        {
                            Code = code ?? "",
                            Success = false,
                            ErrorMessage = "Empty or whitespace code",
                            PalletType = "Unknown"
                        });
                        continue;
                    }

                    // Check for duplicates within the request
                    if (!processedCodes.Add(trimmedCode))
                    {
                        result.FailedUpdates.Add(new PalletUpdateResult
                        {
                            Code = trimmedCode,
                            Success = false,
                            ErrorMessage = "Duplicate code in request",
                            PalletType = "Unknown"
                        });
                        continue;
                    }

                    var updateResult = await ProcessSinglePalletUpdate(trimmedCode, location, dto.StoredBy, username);

                    if (updateResult.Success)
                    {
                        result.SuccessfulUpdates.Add(updateResult);
                        if (!locationUpdated)
                        {
                            location.IsEmpty = false;
                            locationUpdated = true;
                        }
                    }
                    else
                    {
                        result.FailedUpdates.Add(updateResult);
                    }
                }

                // Update location occupancy status if any pallets were successfully updated
                if (result.SuccessfulUpdates.Any())
                {
                    await _locationService.MarkLocationAsOccupiedAsync(location.Id);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                result.ProcessedCount = result.SuccessfulUpdates.Count + result.FailedUpdates.Count;
                result.SuccessCount = result.SuccessfulUpdates.Count;
                result.FailureCount = result.FailedUpdates.Count;

                _logger.LogInformation("Bulk update completed. Total: {Total}, Success: {Success}, Failed: {Failed}",
                    result.TotalRequested, result.SuccessCount, result.FailureCount);

                var message = result.FailureCount == 0
                    ? "All pallets updated successfully"
                    : $"{result.SuccessCount} pallets updated successfully, {result.FailureCount} failed";

                return ApiResponseDto<BulkUpdatePalletLocationResultDto>.SuccessResult(result, message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during bulk pallet location update");

                return ApiResponseDto<BulkUpdatePalletLocationResultDto>.ErrorResult(
                    "An error occurred during bulk update operation",
                    new List<string> { "Database transaction failed" });
            }
        }

        private ValidationResult ValidateBulkUpdateRequest(BulkUpdatePalletLocationDto dto)
        {
            var errors = new List<string>();

            if (dto == null)
            {
                errors.Add("Request data is required");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            if (dto.PalletCodes == null || !dto.PalletCodes.Any())
            {
                errors.Add("At least one pallet code is required");
            }
            else
            {
                // Check for reasonable batch size
                if (dto.PalletCodes.Count > 100) // Configurable limit
                {
                    errors.Add("Maximum 100 pallets can be updated in a single batch");
                }

                // Check for null or empty codes
                var invalidCodes = dto.PalletCodes
                    .Where((code, index) => string.IsNullOrWhiteSpace(code))
                    .Select((code, index) => $"Index {index}")
                    .ToList();

                if (invalidCodes.Any())
                {
                    errors.Add($"Invalid codes found at positions: {string.Join(", ", invalidCodes)}");
                }
            }

            if (string.IsNullOrWhiteSpace(dto.LocationBarcode))
            {
                errors.Add("Location ID is required");
            }

            if (string.IsNullOrWhiteSpace(dto.StoredBy))
            {
                errors.Add("StoredBy is required");
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        private async Task<LocationCapacityResult> ValidateLocationCapacity(Location location, int requestedCount)
        {
            try
            {
                // Get current occupancy
                var currentOccupancy = await _locationService.InventoryCountByLocationIdAsync(location.Id);

                var maxCapacity = location.MaxItems == 0 ? int.MaxValue : location.MaxItems; // Assume unlimited if not set
                var availableCapacity = Math.Max(0, maxCapacity - currentOccupancy);

                return new LocationCapacityResult
                {
                    CanAccommodate = availableCapacity >= requestedCount,
                    AvailableCapacity = availableCapacity,
                    CurrentOccupancy = currentOccupancy,
                    MaxCapacity = maxCapacity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking location capacity for {LocationId}", location.Id);
                // Fail safe - allow the operation but log the error
                return new LocationCapacityResult
                {
                    CanAccommodate = true,
                    AvailableCapacity = int.MaxValue,
                    CurrentOccupancy = 0,
                    MaxCapacity = int.MaxValue
                };
            }
        }

        private async Task<PalletUpdateResult> ProcessSinglePalletUpdate(string code, Location location, string storedBy, string username)
        {
            var updateTime = DateTime.UtcNow;

            // Try FG Pallet first
            var fgPallet = await _dbContext.GIV_FG_ReceivePallets
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.PalletCode == code && !p.IsDeleted);

            if (fgPallet != null)
            {
                var previousLocation = fgPallet.Location?.Code;

                fgPallet.LocationId = location.Id;
                fgPallet.StoredBy = storedBy;
                fgPallet.ModifiedBy = username;
                fgPallet.ModifiedAt = updateTime;

                return new PalletUpdateResult
                {
                    Code = code,
                    Success = true,
                    PalletType = "Finished Goods",
                    PreviousLocation = previousLocation,
                    NewLocation = location.Code
                };
            }

            // Try RM Pallet
            var rmPallet = await _dbContext.GIV_RM_ReceivePallets
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.PalletCode == code && !p.IsDeleted);

            if (rmPallet != null)
            {
                var previousLocation = rmPallet.Location?.Code;

                rmPallet.LocationId = location.Id;
                rmPallet.StoredBy = storedBy;
                rmPallet.ModifiedBy = username;
                rmPallet.ModifiedAt = updateTime;

                return new PalletUpdateResult
                {
                    Code = code,
                    Success = true,
                    PalletType = "Raw Material",
                    PreviousLocation = previousLocation,
                    NewLocation = location.Code
                };
            }

            // Try FG Item
            var fgItem = await _dbContext.GIV_FG_ReceivePalletItems
                .Include(i => i.GIV_FG_ReceivePallet)
                .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(i => i.ItemCode == code && !i.IsDeleted);

            if (fgItem != null)
            {
                var pallet = fgItem.GIV_FG_ReceivePallet;
                var previousLocation = pallet.Location?.Code;

                pallet.LocationId = location.Id;
                pallet.StoredBy = storedBy;
                pallet.ModifiedBy = username;
                pallet.ModifiedAt = updateTime;

                return new PalletUpdateResult
                {
                    Code = code,
                    Success = true,
                    PalletType = "Finished Goods (Item)",
                    PreviousLocation = previousLocation,
                    NewLocation = location.Code
                };
            }

            // Try RM Item
            var rmItem = await _dbContext.GIV_RM_ReceivePalletItems
                .Include(i => i.GIV_RM_ReceivePallet)
                .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(i => i.ItemCode == code && !i.IsDeleted);

            if (rmItem != null)
            {
                var pallet = rmItem.GIV_RM_ReceivePallet;
                var previousLocation = pallet.Location?.Code;

                pallet.LocationId = location.Id;
                pallet.StoredBy = storedBy;
                pallet.ModifiedBy = username;
                pallet.ModifiedAt = updateTime;

                return new PalletUpdateResult
                {
                    Code = code,
                    Success = true,
                    PalletType = "Raw Material (Item)",
                    PreviousLocation = previousLocation,
                    NewLocation = location.Code
                };
            }

            // Not found
            return new PalletUpdateResult
            {
                Code = code,
                Success = false,
                ErrorMessage = $"No pallet or item found with code '{code}'",
                PalletType = "Unknown"
            };
        }

        private async Task MarkLocationAsFull(Guid locationId,string locationbarcode)
        {
            //check location full or not
            var isavailable = await _locationService.CheckLocationAvailabilityByBarcodeAsync(locationbarcode);
            if (!isavailable)
            {
                //if location is full , mark it as occupied
                await _locationService.MarkLocationAsOccupiedAsync(locationId);
            }
        }

        public async Task<ApiResponseDto<string>> ActualReleasePalletLocation(ReleasePalletLocationDto dto, string username, Guid warehouseId)
        {
            using var scope = _logger.BeginScope("▶️ ActualReleasePalletLocation - Code: {Code}, Location: {LocationId}, Warehouse: {WarehouseId}",
        dto.Code, dto.LocationId, warehouseId);

            try
            {
                _logger.LogInformation("Starting ActualReleasePalletLocation for Code: {Code}, Location: {LocationId}, Warehouse: {WarehouseId}",
                dto.Code, dto.LocationId, warehouseId);

                // 1. Validate warehouse
                var warehouseExists = await _dbContext.Warehouses
                    .AnyAsync(w => w.Id == warehouseId && !w.IsDeleted);

                if (!warehouseExists)
                {
                    _logger.LogWarning("Warehouse not found: {WarehouseId}", warehouseId);
                    throw new KeyNotFoundException($"Warehouse with ID {warehouseId} not found.");
                }

                // 2. Validate location
                var location = await _dbContext.Locations
                    .FirstOrDefaultAsync(l => l.Barcode == dto.LocationId && l.WarehouseId == warehouseId && !l.IsDeleted);

                if (location == null)
                {
                    _logger.LogWarning("Location not found: {LocationId} in Warehouse: {WarehouseId}", dto.LocationId, warehouseId);
                    return ApiResponseDto<string>.ErrorResult(
                        $"Location '{dto.LocationId}' not found in warehouse '{warehouseId}.",
                        new List<string> { "Invalid location code." }
                    );
                }

                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    _logger.LogWarning("Empty code provided for release at Location: {LocationId}", dto.LocationId);
                    return ApiResponseDto<string>.ErrorResult(
                        "Code is required.",
                        new List<string> { "No pallet or item code specified." }
                    );
                }

                string inputCode = dto.Code.Trim();

                // Track what we found
                GIV_RM_ReceivePallet? rmPallet = null;
                GIV_FG_ReceivePallet? fgPallet = null;
                GIV_RM_ReceivePalletItem? rmItem = null;
                GIV_FG_ReceivePalletItem? fgItem = null;
                bool isRMItem = false;
                bool isFGItem = false;

                // 3. Try as PalletCode (first RM then FG)
                rmPallet = await _dbContext.GIV_RM_ReceivePallets
                    .Include(p => p.Location)
                    .Include(p => p.RM_ReceivePalletItems)
                    .FirstOrDefaultAsync(p =>
                        p.PalletCode == inputCode &&
                        p.LocationId == location.Id &&
                        !p.IsDeleted);

                if (rmPallet == null)
                {
                    fgPallet = await _dbContext.GIV_FG_ReceivePallets
                        .Include(p => p.Location)
                        .Include(p => p.FG_ReceivePalletItems)
                        .FirstOrDefaultAsync(p =>
                            p.PalletCode == inputCode &&
                            p.LocationId == location.Id &&
                            !p.IsDeleted);
                }

                // 4. If not found as PalletCode, try as ItemCode (first RM then FG)
                if (rmPallet == null && fgPallet == null)
                {
                    rmItem = await _dbContext.GIV_RM_ReceivePalletItems
                        .Include(i => i.GIV_RM_ReceivePallet)
                            .ThenInclude(p => p.Location)
                        .Include(i => i.GIV_RM_ReceivePallet)
                            .ThenInclude(p => p.RM_ReceivePalletItems)
                        .FirstOrDefaultAsync(i =>
                            i.ItemCode == inputCode &&
                            !i.IsDeleted &&
                            i.GIV_RM_ReceivePallet.LocationId == location.Id &&
                            !i.GIV_RM_ReceivePallet.IsDeleted);

                    if (rmItem != null)
                    {
                        rmPallet = rmItem.GIV_RM_ReceivePallet;
                        isRMItem = true;
                    }
                    else
                    {
                        fgItem = await _dbContext.GIV_FG_ReceivePalletItems
                            .Include(i => i.GIV_FG_ReceivePallet)
                                .ThenInclude(p => p.Location)
                            .Include(i => i.GIV_FG_ReceivePallet)
                                .ThenInclude(p => p.FG_ReceivePalletItems)
                            .FirstOrDefaultAsync(i =>
                                i.ItemCode == inputCode &&
                                !i.IsDeleted &&
                                i.GIV_FG_ReceivePallet.LocationId == location.Id &&
                                !i.GIV_FG_ReceivePallet.IsDeleted);

                        if (fgItem != null)
                        {
                            fgPallet = fgItem.GIV_FG_ReceivePallet;
                            isFGItem = true;
                        }
                    }
                }

                string? palletCode = rmPallet?.PalletCode ?? fgPallet?.PalletCode;

                // 5. No match found
                if (rmPallet == null && fgPallet == null)
                {
                    _logger.LogWarning("No matching pallet found with code '{Code}' at location '{LocationId}'", dto.Code, dto.LocationId);
                    return ApiResponseDto<string>.ErrorResult(
                        $"No matching pallet found with code '{dto.Code}' at location '{dto.LocationId}.",
                        new List<string> { "Pallet or item not associated with the specified location." }
                    );
                }

                // Now, check if we're releasing a RM or FG
                if (rmPallet != null)
                {
                    // RM release logic
                    return await HandleRawMaterialRelease(rmPallet, rmItem, isRMItem, location, username);
                }
                else if (fgPallet != null)
                {
                    // FG release logic
                    return await HandleFinishedGoodRelease(fgPallet, fgItem, isFGItem, location, username);
                }

                return ApiResponseDto<string>.ErrorResult("Unknown error processing release request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error occurred in ActualReleasePalletLocation for Code: {Code}, Location: {LocationId}, Warehouse: {WarehouseId}",
                    dto.Code, dto.LocationId, warehouseId);

                return ApiResponseDto<string>.ErrorResult("An unexpected error occurred while processing the release.");
            }
        }

        private async Task<ApiResponseDto<string>> HandleRawMaterialRelease(
    GIV_RM_ReceivePallet pallet,
    GIV_RM_ReceivePalletItem? specificItem,
    bool isItemRelease,
    Location location,
    string username)
        {
            // Find any release detail records for this pallet
            var releaseDetails = await _dbContext.GIV_RM_ReleaseDetails
                .Include(d => d.GIV_RM_Release)
                .Where(d => d.GIV_RM_ReceivePalletId == pallet.Id && !d.IsDeleted)
                .ToListAsync();

            // Get all item IDs for this pallet
            var palletItems = pallet.RM_ReceivePalletItems
                .Where(i => !i.IsDeleted)
                .ToList();

            var palletItemIds = palletItems
                .Select(i => i.Id)
                .ToHashSet();

            // Initialize flags
            bool isInReleaseRecord = false;
            bool isEntirePalletRelease = false;
            bool allItemsInReleaseRecords = false;
            bool someItemsInReleaseRecords = false;
            List<GIV_RM_ReleaseDetails> matchingReleaseDetails = new List<GIV_RM_ReleaseDetails>();

            // First, check if there's an entire pallet release record regardless of item or pallet scanning
            var entirePalletDetails = releaseDetails.Where(d => d.IsEntirePallet).ToList();
            isEntirePalletRelease = entirePalletDetails.Any();

            if (releaseDetails.Any())
            {
                if (isItemRelease && specificItem != null && !isEntirePalletRelease)
                {
                    // Only check individual item record if there's no entire pallet release
                    matchingReleaseDetails = releaseDetails.Where(d => d.GIV_RM_ReceivePalletItemId == specificItem.Id).ToList();
                    isInReleaseRecord = matchingReleaseDetails.Any();
                }

                // Always include entire pallet release details if they exist
                if (isEntirePalletRelease)
                {
                    matchingReleaseDetails.AddRange(entirePalletDetails);
                    isInReleaseRecord = true;
                }
                else if (!isItemRelease) // Scanning a pallet, but no entire pallet release record
                {
                    // Get all items with release records
                    var itemsWithReleaseRecords = releaseDetails
                        .Where(d => d.GIV_RM_ReceivePalletItemId != Guid.Empty)
                        .Select(d => d.GIV_RM_ReceivePalletItemId)
                        .Distinct()
                        .ToHashSet();

                    // Check if ALL items have release records
                    allItemsInReleaseRecords = palletItemIds.Count > 0 && itemsWithReleaseRecords.SetEquals(palletItemIds);

                    // Check if SOME items have release records
                    someItemsInReleaseRecords = itemsWithReleaseRecords.Count > 0;

                    if (allItemsInReleaseRecords || someItemsInReleaseRecords)
                    {
                        // Add all release details for this pallet
                        matchingReleaseDetails = releaseDetails.ToList();
                        isInReleaseRecord = true;

                        if (allItemsInReleaseRecords)
                        {
                            _logger.LogInformation("All items in pallet {PalletCode} have individual release records", pallet.PalletCode);
                        }
                        else
                        {
                            _logger.LogInformation("Some items in pallet {PalletCode} have individual release records", pallet.PalletCode);
                        }
                    }
                }
            }

            if (!isInReleaseRecord && !isEntirePalletRelease && !allItemsInReleaseRecords && !someItemsInReleaseRecords)
            {
                _logger.LogWarning("Pallet/item not found in any release records: PalletId={PalletId}, ItemId={ItemId}",
                    pallet.Id, specificItem?.Id);
                return ApiResponseDto<string>.ErrorResult(
                    "This pallet or item is not scheduled for release.",
                    new List<string> { "Item not found in release records." }
                );
            }

            // Update ActualReleaseDate and ActualReleaseBy on matching release details instead of just ReleaseDate
            var now = DateTime.UtcNow;
            var affectedReleaseIds = new HashSet<Guid>();

            foreach (var detail in matchingReleaseDetails)
            {
                // Update the release detail with actual release information
                detail.ActualReleaseDate = DateTime.UtcNow;
                detail.ActualReleasedBy = username;

                // Keep track of affected release records
                affectedReleaseIds.Add(detail.GIV_RM_ReleaseId);
            }

            string successMessage;

            // If there's an entire pallet release record, always release the entire pallet
            // regardless of whether we scanned an item or the pallet itself
            if (isEntirePalletRelease)
            {
                // Mark the pallet and ALL its items as released
                pallet.IsReleased = true;

                foreach (var item in pallet.RM_ReceivePalletItems)
                {
                    item.IsReleased = true;
                }

                // Clear the location
                pallet.Location!.IsEmpty = true;
                pallet.LocationId = null;
                await _locationService.MarkLocationAsEmptyAsync(location.Id);

                _logger.LogInformation("Entire pallet release record found. Released pallet '{PalletCode}' and all items from location '{LocationCode}'",
                    pallet.PalletCode, location.Code);

                if (isItemRelease && specificItem != null)
                {
                    successMessage = $"Item and entire pallet released from location '{location.Code}' due to entire pallet release record.";
                }
                else
                {
                    successMessage = $"Entire pallet successfully released from location '{location.Code}'.";
                }
            }
            // Handle specific item release (when there's no entire pallet release record)
            else if (isItemRelease && specificItem != null)
            {
                // Count unreleased items before this operation
                int unreleasedItemsBeforeCount = pallet.RM_ReceivePalletItems.Count(i => !i.IsReleased);

                // Mark current item as released
                specificItem.IsReleased = true;
                _logger.LogInformation("Released Raw Material item '{ItemCode}' from pallet '{PalletCode}'",
                    specificItem.ItemCode, pallet.PalletCode);

                // Count unreleased items after this operation
                int unreleasedItemsAfterCount = pallet.RM_ReceivePalletItems.Count(i => !i.IsReleased);

                // Check if this was the last unreleased item
                bool wasLastItem = (unreleasedItemsBeforeCount == 1 && unreleasedItemsAfterCount == 0);

                // Release the pallet location only if this was the last item
                if (wasLastItem)
                {
                    pallet.IsReleased = true;
                    pallet.Location!.IsEmpty = true;
                    pallet.LocationId = null;
                    await _locationService.MarkLocationAsEmptyAsync(location.Id);

                    _logger.LogInformation("Last item released, clearing location for pallet '{PalletCode}'", pallet.PalletCode);
                    successMessage = $"Item released and pallet location cleared from '{location.Code}'.";
                }
                else
                {
                    _logger.LogInformation("Some items still not released, keeping pallet '{PalletCode}' at location '{LocationCode}'",
                        pallet.PalletCode, location.Code);

                    successMessage = $"Item released. Pallet remains at location '{location.Code}' until all items are released.";
                }
            }
            // Handle pallet scanning when all items have individual release records
            else if (allItemsInReleaseRecords)
            {
                // Mark the pallet and all its items as released
                pallet.IsReleased = true;
                foreach (var item in pallet.RM_ReceivePalletItems)
                {
                    item.IsReleased = true;
                }

                // Clear the location
                pallet.Location!.IsEmpty = true;
                pallet.LocationId = null;
                await _locationService.MarkLocationAsEmptyAsync(location.Id);

                _logger.LogInformation("Released entire Raw Material pallet '{PalletCode}' from location '{LocationCode}' (all items had individual release records)",
                    pallet.PalletCode, location.Code);

                successMessage = $"All items were individually scheduled for release. Successfully released entire pallet from location '{location.Code}'.";
            }
            // Handle pallet scanning when SOME items have individual release records
            else if (someItemsInReleaseRecords)
            {
                // Get the IDs of items that have release records
                var itemIdsWithReleaseRecords = releaseDetails
                    .Where(d => d.GIV_RM_ReceivePalletItemId != Guid.Empty)
                    .Select(d => d.GIV_RM_ReceivePalletItemId)
                    .Distinct()
                    .ToHashSet();

                // Release only the items that have release records
                int releasedItemCount = 0;
                foreach (var item in palletItems)
                {
                    if (itemIdsWithReleaseRecords.Contains(item.Id))
                    {
                        item.IsReleased = true;
                        releasedItemCount++;
                    }
                }

                // Check if we've released all items
                bool allItemsNowReleased = palletItems.All(i => i.IsReleased);

                // If all items are now released, we can release the pallet too
                if (allItemsNowReleased)
                {
                    pallet.IsReleased = true;
                    pallet.Location!.IsEmpty = true;
                    pallet.LocationId = null;
                    await _locationService.MarkLocationAsEmptyAsync(location.Id);

                    _logger.LogInformation("All items now released after partial release. Cleared location for pallet '{PalletCode}'",
                        pallet.PalletCode);

                    successMessage = $"Released {releasedItemCount} scheduled items. All items are now released. Pallet location cleared from '{location.Code}'.";
                }
                else
                {
                    _logger.LogInformation("Released {ReleasedCount} of {TotalCount} items from pallet '{PalletCode}'. Pallet remains at location '{LocationCode}'",
                        releasedItemCount, palletItems.Count, pallet.PalletCode, location.Code);

                    successMessage = $"Released {releasedItemCount} of {palletItems.Count} items. Pallet remains at location '{location.Code}' until all items are released.";
                }
            }
            else
            {
                // This should not happen given our previous checks
                successMessage = "Released successfully.";
            }

            // Check if any releases are now complete and update their status
            foreach (var releaseId in affectedReleaseIds)
            {
                await UpdateReleaseCompletionStatus(releaseId, username);
            }

            // Always save changes at the end, regardless of release type
            await _dbContext.SaveChangesAsync();

            return ApiResponseDto<string>.SuccessResult(
                isItemRelease && specificItem != null ? specificItem.ItemCode : pallet.PalletCode!,
                successMessage
            );
        }

        private async Task UpdateReleaseCompletionStatus(Guid releaseId, string username)
        {
            var release = await _dbContext.GIV_RM_Releases
                .Include(r => r.GIV_RM_ReleaseDetails)
                .FirstOrDefaultAsync(r => r.Id == releaseId);

            if (release == null)
                return;

            // Check if all details have been actually released
            bool allDetailsReleased = release.GIV_RM_ReleaseDetails.All(d => d.ActualReleaseDate.HasValue);

            // If all details are released but the release isn't marked as completed yet
            if (allDetailsReleased && !release.ActualReleaseDate.HasValue)
            {
                release.ActualReleaseDate = DateTime.UtcNow;
                release.ActualReleasedBy = username;
                _logger.LogInformation("Release {ReleaseId} marked as fully completed", releaseId);
            }
        }

        private async Task<ApiResponseDto<string>> HandleFinishedGoodRelease(
    GIV_FG_ReceivePallet pallet,
    GIV_FG_ReceivePalletItem? specificItem,
    bool isItemRelease,
    Location location,
    string username)
        {

            // Find any release detail records for this pallet
            var releaseDetails = await _dbContext.GIV_FG_ReleaseDetails
                .Include(d => d.GIV_FG_Release)
                .Where(d => d.GIV_FG_ReceivePalletId == pallet.Id && !d.IsDeleted)
                .ToListAsync();

            // Get all item IDs for this pallet
            var palletItems = pallet.FG_ReceivePalletItems
                .Where(i => !i.IsDeleted)
                .ToList();

            var palletItemIds = palletItems
                .Select(i => i.Id)
                .ToHashSet();

            // Initialize flags
            bool isInReleaseRecord = false;
            bool isEntirePalletRelease = false;
            bool allItemsInReleaseRecords = false;
            bool someItemsInReleaseRecords = false;
            List<GIV_FG_ReleaseDetails> matchingReleaseDetails = new List<GIV_FG_ReleaseDetails>();

            // First, check if there's an entire pallet release record regardless of item or pallet scanning
            var entirePalletDetails = releaseDetails.Where(d => d.IsEntirePallet).ToList();
            isEntirePalletRelease = entirePalletDetails.Any();

            if (releaseDetails.Any())
            {
                if (isItemRelease && specificItem != null && !isEntirePalletRelease)
                {
                    // Only check individual item record if there's no entire pallet release
                    matchingReleaseDetails = releaseDetails.Where(d => d.GIV_FG_ReceivePalletItemId == specificItem.Id).ToList();
                    isInReleaseRecord = matchingReleaseDetails.Any();
                }

                // Always include entire pallet release details if they exist
                if (isEntirePalletRelease)
                {
                    matchingReleaseDetails.AddRange(entirePalletDetails);
                    isInReleaseRecord = true;
                }
                else if (!isItemRelease) // Scanning a pallet, but no entire pallet release record
                {
                    // Get all items with release records
                    var itemsWithReleaseRecords = releaseDetails
                        .Where(d => d.GIV_FG_ReceivePalletItemId != null && d.GIV_FG_ReceivePalletItemId != Guid.Empty)
                        .Select(d => d.GIV_FG_ReceivePalletItemId!.Value)
                        .Distinct()
                        .ToHashSet();

                    // Check if ALL items have release records
                    allItemsInReleaseRecords = palletItemIds.Count > 0 && itemsWithReleaseRecords.SetEquals(palletItemIds);

                    // Check if SOME items have release records
                    someItemsInReleaseRecords = itemsWithReleaseRecords.Count > 0;

                    if (allItemsInReleaseRecords || someItemsInReleaseRecords)
                    {
                        // Add all release details for this pallet
                        matchingReleaseDetails = releaseDetails.ToList();
                        isInReleaseRecord = true;

                        if (allItemsInReleaseRecords)
                        {
                            _logger.LogInformation("All items in pallet {PalletCode} have individual release records", pallet.PalletCode);
                        }
                        else
                        {
                            _logger.LogInformation("Some items in pallet {PalletCode} have individual release records", pallet.PalletCode);
                        }
                    }
                }
            }

            if (!isInReleaseRecord && !isEntirePalletRelease && !allItemsInReleaseRecords && !someItemsInReleaseRecords)
            {
                _logger.LogWarning("FG pallet/item not found in any release records: PalletId={PalletId}, ItemId={ItemId}",
                    pallet.Id, specificItem?.Id);
                return ApiResponseDto<string>.ErrorResult(
                    "This finished good pallet or item is not scheduled for release.",
                    new List<string> { "Item not found in release records." }
                );
            }

            // Update ActualReleaseDate and ActualReleaseBy on matching release details instead of just ReleaseDate
            var now = DateTime.UtcNow;
            var affectedReleaseIds = new HashSet<Guid>();

            foreach (var detail in matchingReleaseDetails)
            {
                // Update the release detail with actual release information
                detail.ActualReleaseDate = DateTime.UtcNow;
                detail.ActualReleasedBy = username;

                // Keep track of affected release records
                affectedReleaseIds.Add(detail.GIV_FG_ReleaseId);
            }

            string successMessage;

            // If there's an entire pallet release record, always release the entire pallet
            // regardless of whether we scanned an item or the pallet itself
            if (isEntirePalletRelease)
            {
                // Mark the pallet and ALL its items as released
                pallet.IsReleased = true;

                foreach (var item in pallet.FG_ReceivePalletItems)
                {
                    item.IsReleased = true;
                }

                // Clear the location
                pallet.Location!.IsEmpty = true;
                pallet.LocationId = null;
                await _locationService.MarkLocationAsEmptyAsync(location.Id);

                _logger.LogInformation("Entire pallet release record found. Released FG pallet '{PalletCode}' and all items from location '{LocationCode}'",
                    pallet.PalletCode, location.Code);

                if (isItemRelease && specificItem != null)
                {
                    successMessage = $"Item and entire pallet released from location '{location.Code}' due to entire pallet release record.";
                }
                else
                {
                    successMessage = $"Entire pallet successfully released from location '{location.Code}'.";
                }
            }
            // Handle specific item release (when there's no entire pallet release record)
            else if (isItemRelease && specificItem != null)
            {
                // Count unreleased items before this operation
                int unreleasedItemsBeforeCount = pallet.FG_ReceivePalletItems.Count(i => !i.IsReleased);

                // Mark current item as released
                specificItem.IsReleased = true;
                _logger.LogInformation("Released Finished Good item '{ItemCode}' from pallet '{PalletCode}'",
                    specificItem.ItemCode, pallet.PalletCode);

                // Count unreleased items after this operation
                int unreleasedItemsAfterCount = pallet.FG_ReceivePalletItems.Count(i => !i.IsReleased);

                // Check if this was the last unreleased item
                bool wasLastItem = (unreleasedItemsBeforeCount == 1 && unreleasedItemsAfterCount == 0);

                // Release the pallet location only if this was the last item
                if (wasLastItem)
                {
                    pallet.IsReleased = true;
                    pallet.Location!.IsEmpty = true;
                    pallet.LocationId = null;
                    await _locationService.MarkLocationAsEmptyAsync(location.Id);

                    _logger.LogInformation("Last item released, clearing location for FG pallet '{PalletCode}'", pallet.PalletCode);
                    successMessage = $"Item released and pallet location cleared from '{location.Code}'.";
                }
                else
                {
                    _logger.LogInformation("Some items still not released, keeping FG pallet '{PalletCode}' at location '{LocationCode}'",
                        pallet.PalletCode, location.Code);

                    successMessage = $"Item released. Pallet remains at location '{location.Code}' until all items are released.";
                }
            }
            // Handle pallet scanning when all items have individual release records
            else if (allItemsInReleaseRecords)
            {
                // Mark the pallet and all its items as released
                pallet.IsReleased = true;
                foreach (var item in pallet.FG_ReceivePalletItems)
                {
                    item.IsReleased = true;
                }

                // Clear the location
                pallet.Location!.IsEmpty = true;
                pallet.LocationId = null;
                await _locationService.MarkLocationAsEmptyAsync(location.Id);

                _logger.LogInformation("Released entire Finished Good pallet '{PalletCode}' from location '{LocationCode}' (all items had individual release records)",
                    pallet.PalletCode, location.Code);

                successMessage = $"All items were individually scheduled for release. Successfully released entire pallet from location '{location.Code}'.";
            }
            // Handle pallet scanning when SOME items have individual release records
            else if (someItemsInReleaseRecords)
            {
                // Get the IDs of items that have release records
                var itemIdsWithReleaseRecords = releaseDetails
                    .Where(d => d.GIV_FG_ReceivePalletItemId != null && d.GIV_FG_ReceivePalletItemId != Guid.Empty)
                    .Select(d => d.GIV_FG_ReceivePalletItemId!.Value)
                    .Distinct()
                    .ToHashSet();

                // Release only the items that have release records
                int releasedItemCount = 0;
                foreach (var item in palletItems)
                {
                    if (itemIdsWithReleaseRecords.Contains(item.Id))
                    {
                        item.IsReleased = true;
                        releasedItemCount++;
                    }
                }

                // Check if we've released all items
                bool allItemsNowReleased = palletItems.All(i => i.IsReleased);

                // If all items are now released, we can release the pallet too
                if (allItemsNowReleased)
                {
                    pallet.IsReleased = true;
                    pallet.Location!.IsEmpty = true;
                    pallet.LocationId = null;
                    await _locationService.MarkLocationAsEmptyAsync(location.Id);

                    _logger.LogInformation("All items now released after partial release. Cleared location for FG pallet '{PalletCode}'",
                        pallet.PalletCode);

                    successMessage = $"Released {releasedItemCount} scheduled items. All items are now released. Pallet location cleared from '{location.Code}'.";
                }
                else
                {
                    _logger.LogInformation("Released {ReleasedCount} of {TotalCount} items from FG pallet '{PalletCode}'. Pallet remains at location '{LocationCode}'",
                        releasedItemCount, palletItems.Count, pallet.PalletCode, location.Code);

                    successMessage = $"Released {releasedItemCount} of {palletItems.Count} items. Pallet remains at location '{location.Code}' until all items are released.";
                }
            }
            else
            {
                // This should not happen given our previous checks
                successMessage = "Released successfully.";
            }

            // Check if any releases are now complete and update their status
            foreach (var releaseId in affectedReleaseIds)
            {
                await UpdateFGReleaseCompletionStatus(releaseId, username);
            }

            // Always save changes at the end, regardless of release type
            await _dbContext.SaveChangesAsync();

            return ApiResponseDto<string>.SuccessResult(
                isItemRelease && specificItem != null ? specificItem.ItemCode : pallet.PalletCode!,
                successMessage
            );
        }

        // New helper method to update FG release completion status
        private async Task UpdateFGReleaseCompletionStatus(Guid releaseId, string username)
        {
            var release = await _dbContext.GIV_FG_Releases
                .Include(r => r.GIV_FG_ReleaseDetails)
                .FirstOrDefaultAsync(r => r.Id == releaseId);

            if (release == null)
                return;

            // Check if all details have been actually released
            bool allDetailsReleased = release.GIV_FG_ReleaseDetails.All(d => d.ActualReleaseDate.HasValue);

            // If all details are released but the release isn't marked as completed yet
            if (allDetailsReleased && !release.ActualReleaseDate.HasValue)
            {
                release.ActualReleaseDate = DateTime.UtcNow;
                release.ActualReleasedBy = username;
                _logger.LogInformation("FG Release {ReleaseId} marked as fully completed", releaseId);
            }
        }
    }
}
