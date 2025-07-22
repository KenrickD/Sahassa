using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Warehouses;

namespace WMS.Application.Interfaces
{
    public interface IWarehouseService
    {
        Task<PaginatedResult<WarehouseListItemDto>> GetPaginatedWarehousesAsync(
             string searchTerm, int skip, int take, int sortColumn, bool sortAscending);

        Task<List<WarehouseDto>> GetAllWarehousesAsync();

        Task<WarehouseDto> GetWarehouseByIdAsync(Guid id);

        Task<WarehouseDto> CreateWarehouseAsync(WarehouseCreateDto warehouseDto);

        Task<WarehouseDto> UpdateWarehouseAsync(Guid id, WarehouseUpdateDto warehouseDto);

        Task<bool> DeleteWarehouseAsync(Guid id);

        Task<bool> ActivateWarehouseAsync(Guid id, bool isActive);

        Task<WarehouseConfigurationDto> GetWarehouseConfigurationAsync(Guid warehouseId);

        Task<WarehouseConfigurationDto> UpdateWarehouseConfigurationAsync(Guid warehouseId,
            WarehouseConfigurationUpdateDto configDto);

        Task<bool> WarehouseExistsAsync(Guid id);

        Task<bool> WarehouseNameExistsAsync(string name, Guid? excludeId = null);

        Task<bool> WarehouseCodeExistsAsync(string code, Guid? excludeId = null);
    }
}