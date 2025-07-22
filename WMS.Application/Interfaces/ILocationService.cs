using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Models;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Application.Interfaces
{
    public interface ILocationService
    {
        // Pagination method
        Task<PaginatedResult<LocationDto>> GetPaginatedLocations(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending, Guid? warehouseId = null, Guid? zoneId = null);

        //Get location
        Task<List<LocationDto>> GetAllLocationsAsync();
        Task<List<LocationDto>> GetLocationsByZoneIdAsync(Guid zoneId);
        Task<List<LocationDto>> GetLocationsByWarehouseIdAsync(Guid warehouseId);
        Task<LocationDto> GetLocationByIdAsync(Guid id);
        Task<List<LocationDto>> GetLocationsByZoneIdAsync(Guid zoneId, bool activeOnly = false);
        Task<List<LocationDto>> GetEmptyLocationsAsync(Guid? zoneId = null);
        Task<List<LocationDto>> GetLocationsByTypeAsync(LocationType locationType, Guid? zoneId = null);
        Task<int> GetLocationCountByZoneAsync(Guid zoneId);
        Task<int> GetEmptyLocationCountByZoneAsync(Guid zoneId);
        Task<Location?> GetByZoneIdAndCodeAsync(Guid zoneId, string locationCode);
        Task<List<Location>> GetByZoneIdAndCodesAsync(Guid zoneId, List<string> locationCodes);
        Task<Location?> GetByIdAsync(Guid id);
        Task<Location?> GetByBarcodeAsync(string barcode);
        Task<int> InventoryCountByLocationIdAsync(Guid locationId);
        // Search and filter operations
        Task<List<LocationDto>> SearchLocationsAsync(string searchTerm, Guid? zoneId = null, LocationType? type = null, bool? isEmpty = null);
        Task<List<LocationDto>> GetLocationsByRowAsync(Guid zoneId, string row);
        Task<List<LocationDto>> GetLocationsByBayAsync(Guid zoneId, int bay);
        Task<List<LocationDto>> GetLocationsByLevelAsync(Guid zoneId, int level);

        // Standard CRUD operations
        Task<LocationDto> CreateLocationAsync(LocationCreateDto locationDto);
        Task<LocationDto> UpdateLocationAsync(Guid id, LocationUpdateDto locationDto);
        Task<bool> DeleteLocationAsync(Guid id);
        Task<bool> ActivateLocationAsync(Guid id, bool isActive);

        // Validation methods
        Task<bool> LocationExistsAsync(Guid id);
        Task<bool> LocationNameExistsAsync(string name, Guid? excludeId = null);
        Task<bool> LocationCodeExistsAsync(string code, Guid? excludeId = null);
        Task<bool> LocationBarcodeExistsAsync(string barcode, Guid? excludeId = null);
        Task<bool> LocationNameExistsInZoneAsync(string name, Guid zoneId, Guid? excludeId = null);
        Task<bool> LocationCodeExistsInZoneAsync(string code, Guid zoneId, Guid? excludeId = null);
        Task<bool> CheckLocationAvailabilityByBarcodeAsync(string barcode);
        // Position-based validation
        Task<bool> LocationPositionExistsAsync(Guid zoneId, string row, int bay, int level, Guid? excludeId = null);

        // Location-specific operations
        Task<bool> MarkLocationAsEmptyAsync(Guid locationId);
        Task<bool> MarkLocationAsOccupiedAsync(Guid locationId);
        Task<bool> MarkLocationAsEmptyByBarcodeAsync(string barcode);
        Task<bool> MarkLocationAsOccupiedByBarcodeAsync(string barcode);
        Task<decimal> GetLocationUtilizationAsync(Guid locationId);
        Task<List<LocationDto>> GetLocationsByAccessTypeAsync(AccessType accessType, Guid? zoneId = null);

        // Bulk operations
        Task<bool> BulkCreateLocationsAsync(List<LocationCreateDto> locations);
        Task<bool> BulkUpdateLocationStatusAsync(List<Guid> locationIds, bool isActive);

        // Reporting methods
        Task<Dictionary<string, int>> GetLocationCountByStatusAsync(Guid? zoneId = null);
        Task<Dictionary<LocationType, int>> GetLocationCountByTypeAsync(Guid? zoneId = null);
        Task<List<LocationDto>> GetTopUtilizedLocationsAsync(int count = 10, Guid? zoneId = null);

        // Import/Export methods
        byte[] GenerateLocationTemplate();
        Task<byte[]> ExportLocationsAsync(Guid? zoneId = null);
        Task<LocationImportValidationResult> ValidateLocationImportAsync(IFormFile file);
        Task<LocationImportResult> ImportLocationsAsync(IFormFile file);

        #region Location Grid Modal UI link inventory
        Task<GetLinkableItemsResponseDto> GetAvailableLinkableItemsAsync(GetLinkableItemsRequestDto request);
        Task<LinkItemsToLocationResponseDto> LinkItemsToLocationAsync(LinkItemsToLocationRequestDto request);
        #endregion

    }
}