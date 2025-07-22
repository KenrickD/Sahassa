using WMS.Domain.DTOs.Zones;
using WMS.Domain.DTOs;

namespace WMS.Application.Interfaces
{
    public interface IZoneService
    {
        // Pagination method
        Task<PaginatedResult<ZoneDto>> GetPaginatedZones(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending);

        Task<List<ZoneDto>> GetAllZonesAsync();
        Task<List<ZoneDto>> GetZonesByWarehouseIdAsync(Guid warehouseId);
        Task<ZoneDto> GetZoneByIdAsync(Guid id);
        Task<ZoneDto> CreateZoneAsync(ZoneCreateDto zoneDto);
        Task<ZoneDto> UpdateZoneAsync(Guid id, ZoneUpdateDto zoneDto);
        Task<bool> DeleteZoneAsync(Guid id);
        Task<bool> ActivateZoneAsync(Guid id, bool isActive);

        // Validation methods
        Task<bool> ZoneExistsAsync(Guid id);
        Task<bool> ZoneNameExistsAsync(string name, Guid? excludeId = null);
        Task<bool> ZoneCodeExistsAsync(string code, Guid? excludeId = null);
        Task<bool> ZoneNameExistsInWarehouseAsync(string name, Guid warehouseId, Guid? excludeId = null);
        Task<bool> ZoneCodeExistsInWarehouseAsync(string code, Guid warehouseId, Guid? excludeId = null);

        // Additional methods
        Task<List<ZoneDto>> GetZonesByWarehouseIdAsync(Guid warehouseId, bool activeOnly = false);
        Task<int> GetZoneCountByWarehouseAsync(Guid warehouseId);
    }
}
