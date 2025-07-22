using System.Threading.Tasks;
using WMS.Application.Interfaces;
using WMS.Application.Services;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.SignalR;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;

namespace WMS.Application.Helpers
{
    public class LocationGridHelper
    {
        private readonly ILocationService _locationService;
        private readonly ICurrentUserService _currentUserService;
        public LocationGridHelper(ILocationService locationService, ICurrentUserService currentUserService)
        {
            _locationService = locationService;
            _currentUserService = currentUserService;
        }
        public async Task<LocationUpdateDto> GetLocationUpdateDtoByZoneIdAndLocationCodeAsync(Guid zoneId, string locationCode)
        {
            var location = await _locationService.GetByZoneIdAndCodeAsync(zoneId, locationCode);
            
            if (location == null)
                return new LocationUpdateDto();
            var update = MapToLocationUpdateDto(location);

            return update;
        }
        public async Task<LocationUpdateDto> GetLocationUpdateDtoByLocationIdAsync(Guid locationId)
        {
            var location = await _locationService.GetByIdAsync(locationId);

            if (location == null)
                return new LocationUpdateDto();

            return MapToLocationUpdateDto(location);
        }

        public async Task<List<LocationUpdateDto>> GetLocationUpdateDtosByZoneIdAndLocationCodesAsync(Guid zoneId, List<string> locationCodes)
        {
            List<LocationUpdateDto> updateDtos = new List<LocationUpdateDto>();
            var locations = await _locationService.GetByZoneIdAndCodesAsync(zoneId, locationCodes);

            foreach (var location in locations)
            {
                var update = MapToLocationUpdateDto(location);

                updateDtos.Add(update);
            }

            return updateDtos;
        }

        private LocationUpdateDto MapToLocationUpdateDto(Location location)
        {
            var inventoryCount = location.Inventories.Count() + location.GIVFGReceivePallets.Count() + location.GIVRMReceivePallets.Count();
            var updateDto = new LocationUpdateDto
            {
                LocationId = location.Id,
                LocationCode = location.Code,
                ZoneId = location.ZoneId,
                WarehouseId = location.Zone.WarehouseId,
                IsEmpty = location.IsEmpty,
                Status = (location.IsEmpty || inventoryCount == 0) ? AppConsts.LocationGridStatus.AVAILABLE : 
                    (inventoryCount < location.MaxItems ? AppConsts.LocationGridStatus.PARTIAL : AppConsts.LocationGridStatus.OCCUPIED),
                InventoryCount = inventoryCount,
                TotalQuantity = location.Inventories?.Where(i => !i.IsDeleted).Sum(i => i.Quantity) ?? 0,
                UpdateType = LocationUpdateType.StatusChanged,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = _currentUserService.GetCurrentUsername,
                Row = location.Row ?? "",
                Bay = location.Bay ?? 0,
                Level = location.Level ?? 0
            };

            return updateDto;
        }
    }
}
