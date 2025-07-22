using Amazon.Runtime.Internal;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using WMS.Application.Extensions;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Clients;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly IAPIService _apiService;
        private readonly ILogger<LocationService> _logger;

        public LocationService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IDateTime dateTime,
            IAPIService apiService,
            ILogger<LocationService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<PaginatedResult<LocationDto>> GetPaginatedLocations(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending, Guid? warehouseId = null, Guid? zoneId = null)
        {
            _logger.LogDebug("Getting paginated locations: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                searchTerm, skip, take, sortColumn, sortAscending);

            try
            {
                // Start with base query using tenant filter
                var query = _dbContext.Locations
                    .ApplyTenantFilter(_currentUserService)
                    .Include(l => l.Zone)
                        .ThenInclude(z => z.Warehouse)
                    .AsQueryable();

                // Apply warehouse filter
                if (warehouseId.HasValue)
                {
                    query = query.Where(l => l.Zone.WarehouseId == warehouseId.Value);
                    _logger.LogDebug("Applied warehouse filter: {WarehouseId}", warehouseId.Value);
                }

                // Apply zone filter
                if (zoneId.HasValue)
                {
                    query = query.Where(l => l.ZoneId == zoneId.Value);
                    _logger.LogDebug("Applied zone filter: {ZoneId}", zoneId.Value);
                }

                // Apply search if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogDebug("Applying search filter: {SearchTerm}", searchTerm);

                    query = query.Where(l =>
                        l.Name.ToLower().Contains(searchTerm) ||
                        l.Code.ToLower().Contains(searchTerm) ||
                        (l.Zone != null && l.Zone.Name.ToLower().Contains(searchTerm)) ||
                        (l.Zone != null && l.Zone.Warehouse != null && l.Zone.Warehouse.Name.ToLower().Contains(searchTerm)) ||
                        (l.Row != null && l.Row.ToLower().Contains(searchTerm)) ||
                        (l.FullLocationCode != null && l.FullLocationCode.ToLower().Contains(searchTerm))
                    );
                }

                // Apply sorting
                query = ApplySorting(query, sortColumn, !sortAscending);

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total locations matching criteria: {TotalCount}", totalCount);

                // Apply pagination
                var locations = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var locationDtos = _mapper.Map<List<LocationDto>>(locations);

                // Get inventory counts for each location
                foreach (var locationDto in locationDtos)
                {
                    var inventoryCounts = await InventoryCountByLocationIdAsync(locationDto.Id);
                    locationDto.InventoryCount = inventoryCounts;

                    // Calculate utilization
                    locationDto.CurrentUtilization = await CalculateLocationUtilizationAsync(locationDto.Id);
                }

                // Update FullLocationCode and IsEmpty status
                foreach (var location in locations)
                {
                    location.FullLocationCode = GenerateFullLocationCode(location);
                    var inventoryCounts = await InventoryCountByLocationIdAsync(location.Id);

                    location.IsEmpty = inventoryCounts == 0;
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Retrieved {LocationCount} paginated locations (skip={Skip}, take={Take}) from total of {TotalCount}",
                    locationDtos.Count, skip, take, totalCount);

                return new PaginatedResult<LocationDto>
                {
                    Items = locationDtos,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated locations: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private IQueryable<Location> ApplySorting(IQueryable<Location> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(l => l.Zone.Warehouse.Name) : query.OrderBy(l => l.Zone.Warehouse.Name),
                2 => sortDescending ? query.OrderByDescending(l => l.Zone.Name) : query.OrderBy(l => l.Zone.Name),
                3 => sortDescending ? query.OrderByDescending(l => l.Name) : query.OrderBy(l => l.Name),
                4 => sortDescending ? query.OrderByDescending(l => l.Code) : query.OrderBy(l => l.Code),
                5 => sortDescending ? query.OrderByDescending(l => l.Row).ThenByDescending(l => l.Bay).ThenByDescending(l => l.Level) :
                                     query.OrderBy(l => l.Row).ThenBy(l => l.Bay).ThenBy(l => l.Level),
                6 => sortDescending ? query.OrderByDescending(l => l.IsEmpty) : query.OrderBy(l => l.IsEmpty),
                7 => sortDescending ? query.OrderByDescending(l => l.IsActive) : query.OrderBy(l => l.IsActive),
                _ => sortDescending ? query.OrderByDescending(l => l.Name) : query.OrderBy(l => l.Name)
            };
        }

        public async Task<LocationDto> CreateLocationAsync(LocationCreateDto locationDto)
        {
            _logger.LogInformation("Creating new location: {LocationName}", locationDto.Name);

            // Validate unique constraints
            if (await LocationNameExistsInZoneAsync(locationDto.Name, locationDto.ZoneId))
            {
                var message = $"Location name '{locationDto.Name}' already exists in this zone";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (await LocationCodeExistsInZoneAsync(locationDto.Code, locationDto.ZoneId))
            {
                var message = $"Location code '{locationDto.Code}' already exists in this zone";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (!string.IsNullOrEmpty(locationDto.Barcode))
            {
                if (await LocationBarcodeExistsAsync(locationDto.Barcode))
                {
                    var message = $"Location barcode '{locationDto.Barcode}' already exists";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }
            }

            // Validate position uniqueness if provided
            if (!string.IsNullOrEmpty(locationDto.Row) && locationDto.Bay.HasValue && locationDto.Level.HasValue)
            {
                if (await LocationPositionExistsAsync(locationDto.ZoneId, locationDto.Row, locationDto.Bay.Value, locationDto.Level.Value))
                {
                    var message = $"Location position Row {locationDto.Row}, Bay {locationDto.Bay}, Level {locationDto.Level} already exists in this zone";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var location = _mapper.Map<Location>(locationDto);
                location.Id = Guid.NewGuid();
                location.IsEmpty = true; // New location is empty by default

                location.Code = locationDto.Code.ToUpper();
                location.Name = locationDto.Name.ToUpper();
                location.Row = locationDto.Row?.ToUpper();

                // Generate full location code
                location.FullLocationCode = GenerateFullLocationCode(location);

                await _dbContext.AddAsync(location);
                await _dbContext.SaveChangesAsync();

                // Reload location with zone and warehouse information
                await _dbContext.Entry(location).Reference(l => l.Zone).LoadAsync();
                await _dbContext.Entry(location.Zone).Reference(z => z.Warehouse).LoadAsync();

                await transaction.CommitAsync();

                var result = _mapper.Map<LocationDto>(location);
                result.InventoryCount = 0; // New location has no inventory
                result.CurrentUtilization = 0;

                _logger.LogInformation("Location created successfully: {LocationId} - {LocationName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating location: {LocationName}", locationDto.Name);
                throw;
            }
        }

        public async Task<LocationDto> UpdateLocationAsync(Guid id, LocationUpdateDto locationDto)
        {
            _logger.LogInformation("Updating location: {LocationId} - {LocationName}", id, locationDto.Name);

            // Validate unique constraints
            if (await LocationNameExistsAsync(locationDto.Name, id))
            {
                var message = $"Location name '{locationDto.Name}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (await LocationCodeExistsAsync(locationDto.Code, id))
            {
                var message = $"Location code '{locationDto.Code}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (!string.IsNullOrEmpty(locationDto.Barcode))
            {
                if (await LocationBarcodeExistsAsync(locationDto.Barcode, id))
                {
                    var message = $"Location barcode '{locationDto.Barcode}' already exists";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                //var location = await _dbContext.Locations
                //    .ApplyTenantFilter(_currentUserService)
                //    .Include(l => l.Zone)
                //        .ThenInclude(z => z.Warehouse)
                //    .FirstOrDefaultAsync(l => l.Id == id);
                var location = await _dbContext.Locations.FindAsync(id);

                if (location == null)
                {
                    var message = $"Location with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Update location properties
                _mapper.Map(locationDto, location);

                location.Code = locationDto.Code.ToUpper();
                location.Name = locationDto.Name.ToUpper();
                location.Row = locationDto.Row?.ToUpper();

                // Regenerate full location code
                location.FullLocationCode = GenerateFullLocationCode(location);

                _dbContext.Update(location);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = _mapper.Map<LocationDto>(location);

                // Get inventory count and utilization
                result.InventoryCount = await _dbContext.Inventories
                    .CountAsync(i => i.LocationId == id && !i.IsDeleted);
                result.CurrentUtilization = await CalculateLocationUtilizationAsync(id);

                _logger.LogInformation("Location updated successfully: {LocationId} - {LocationName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating location: {LocationId} - {LocationName}", id, locationDto.Name);
                throw;
            }
        }

        public async Task<bool> DeleteLocationAsync(Guid id)
        {
            _logger.LogInformation("Deleting location: {LocationId}", id);

            try
            {
                var location = await _dbContext.Locations
                    .ApplyTenantFilter(_currentUserService)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (location == null)
                {
                    var message = $"Location with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Check if location has associated inventory
                var hasInventory = await _dbContext.Inventories.AnyAsync(i => i.LocationId == id && !i.IsDeleted);

                if (hasInventory)
                {
                    var message = $"Cannot delete location '{location.Name}' as it has associated inventory";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                location.IsDeleted = true;
                location.ModifiedBy = _currentUserService.UserId;
                location.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Location deleted successfully: {LocationId} - {LocationName}",
                    id, location.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location: {LocationId}", id);
                throw;
            }
        }

        // Additional implementations
        public async Task<LocationDto> GetLocationByIdAsync(Guid id)
        {
            var location = await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Include(l => l.Zone)
                    .ThenInclude(z => z.Warehouse)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                throw new KeyNotFoundException($"Location with ID {id} not found.");
            }

            var result = _mapper.Map<LocationDto>(location);

            result.InventoryCount = await InventoryCountByLocationIdAsync(id);
            result.CurrentUtilization = await CalculateLocationUtilizationAsync(id);

            return result;
        }

        public async Task<bool> LocationPositionExistsAsync(Guid zoneId, string row, int bay, int level, Guid? excludeId = null)
        {
            return await _dbContext.Locations
                .AnyAsync(l => l.ZoneId == zoneId &&
                              l.Row == row &&
                              l.Bay == bay &&
                              l.Level == level &&
                              !l.IsDeleted &&
                              (excludeId == null || l.Id != excludeId));
        }

        public async Task<bool> BulkCreateLocationsAsync(List<LocationCreateDto> locations)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var locationEntities = new List<Location>();

                foreach (var locationDto in locations)
                {
                    var location = _mapper.Map<Location>(locationDto);
                    location.Id = Guid.NewGuid();
                    location.CreatedBy = _currentUserService.UserId;
                    location.CreatedAt = _dateTime.Now;
                    location.IsEmpty = true;
                    location.FullLocationCode = GenerateFullLocationCode(location);

                    locationEntities.Add(location);
                }

                await _dbContext.Locations.AddRangeAsync(locationEntities);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Bulk created {Count} locations", locationEntities.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error bulk creating locations");
                throw;
            }
        }

        // Helper methods
        private string GenerateFullLocationCode(Location location)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(location.Row)) parts.Add(location.Row);
            if (location.Bay.HasValue) parts.Add(location.Bay.Value.ToString("00"));
            if (location.Level.HasValue) parts.Add(location.Level.Value.ToString("00"));

            return parts.Any() ? string.Join("-", parts) : location.Code;
        }
        public async Task<int> InventoryCountByLocationIdAsync(Guid locationId)
        {
            var invCount = await _dbContext.Inventories.CountAsync(i => i.LocationId == locationId && !i.IsDeleted);
            var givFGCount = await _dbContext.GIV_FG_ReceivePallets.CountAsync(i => i.LocationId == locationId && !i.IsDeleted);
            var givRMCount = await _dbContext.GIV_RM_ReceivePallets.CountAsync(i => i.LocationId == locationId && !i.IsDeleted);

            return invCount + givFGCount + givRMCount;
        }

        private async Task<decimal> CalculateLocationUtilizationAsync(Guid locationId)
        {
            var location = await _dbContext.Locations.FindAsync(locationId);
            if (location == null || location.MaxItems == 0) return 0;

            var currentItems = await _dbContext.Inventories
                .CountAsync(i => i.LocationId == locationId && !i.IsDeleted);

            return Math.Round((decimal)currentItems / location.MaxItems * 100, 2);
        }

        public async Task<List<LocationDto>> GetAllLocationsAsync()
        {
            var locations = await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Include(l => l.Zone)
                    .ThenInclude(z => z.Warehouse)
                .ToListAsync();

            return _mapper.Map<List<LocationDto>>(locations);
        }

        public async Task<List<LocationDto>> GetLocationsByZoneIdAsync(Guid zoneId)
        {
            var locations = await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Include(l => l.Zone)
                    .ThenInclude(z => z.Warehouse)
                .Where(l => l.ZoneId == zoneId)
                .ToListAsync();

            return _mapper.Map<List<LocationDto>>(locations);
        }
        public async Task<Location?> GetByZoneIdAndCodeAsync(Guid zoneId, string locationCode)
        {
            var location = await _dbContext.Locations
               .Include(l => l.Zone)
               .Include(l => l.Inventories.Where(i => !i.IsDeleted))
               .Include(l => l.GIVRMReceivePallets.Where(i => !i.IsDeleted))
               .Include(l => l.GIVFGReceivePallets.Where(i => !i.IsDeleted))
               .Where(l => l.ZoneId == zoneId &&
                           l.Code == locationCode &&
                           !l.IsDeleted)
               .FirstOrDefaultAsync();
            return location;
        }
        public async Task<List<Location>> GetByZoneIdAndCodesAsync(Guid zoneId, List<string> locationCodes)
        {
            var locations = await _dbContext.Locations
               .Include(l => l.Zone)
               .Include(l => l.Inventories.Where(i => !i.IsDeleted))
               .Include(l => l.GIVRMReceivePallets.Where(i => !i.IsDeleted))
               .Include(l => l.GIVFGReceivePallets.Where(i => !i.IsDeleted))
               .Where(l => l.ZoneId == zoneId &&
                           locationCodes.Contains(l.Code) &&
                           !l.IsDeleted)
               .ToListAsync();
            return locations;
        }
        public async Task<Location?> GetByIdAsync(Guid id)
        {
            var location = await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
               .Include(l => l.Zone)
               .Include(l => l.Inventories.Where(i => !i.IsDeleted))
               .Include(l => l.GIVRMReceivePallets.Where(i => !i.IsDeleted))
               .Include(l => l.GIVFGReceivePallets.Where(i => !i.IsDeleted))
               .Where(l => l.Id == id &&
                           !l.IsDeleted)
               .FirstOrDefaultAsync();
            return location;
        }
        public async Task<Location?> GetByBarcodeAsync(string barcode)
        {
            var location = await _dbContext.Locations
               .Include(l => l.Zone)
               .Where(l => l.Barcode == barcode &&
                           !l.IsDeleted)
               .FirstOrDefaultAsync();
            return location;
        }
        public async Task<bool> LocationExistsAsync(Guid id)
        {
            return await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .AnyAsync(l => l.Id == id);
        }

        public async Task<bool> LocationNameExistsAsync(string name, Guid? excludeId = null)
        {
            var warehouseId = _currentUserService.CurrentWarehouseId;
            return await _dbContext.Locations
                .AnyAsync(l => l.Name == name &&
                              l.WarehouseId == warehouseId &&
                              !l.IsDeleted &&
                              (excludeId == null || l.Id != excludeId));
        }

        public async Task<bool> LocationCodeExistsAsync(string code, Guid? excludeId = null)
        {
            var warehouseId = _currentUserService.CurrentWarehouseId;
            return await _dbContext.Locations
                .AnyAsync(l => l.Code == code &&
                              l.WarehouseId == warehouseId &&
                              !l.IsDeleted &&
                              (excludeId == null || l.Id != excludeId));
        }

        public async Task<bool> LocationBarcodeExistsAsync(string barcode, Guid? excludeId = null)
        {
            return await _dbContext.Locations
                .AnyAsync(l => l.Barcode == barcode &&
                              !l.IsDeleted &&
                              (excludeId == null || l.Id != excludeId));
        }

        public async Task<bool> LocationNameExistsInZoneAsync(string name, Guid zoneId, Guid? excludeId = null)
        {
            return await _dbContext.Locations
                .AnyAsync(l => l.Name == name &&
                              l.ZoneId == zoneId &&
                              !l.IsDeleted &&
                              (excludeId == null || l.Id != excludeId));
        }

        public async Task<bool> LocationCodeExistsInZoneAsync(string code, Guid zoneId, Guid? excludeId = null)
        {
            return await _dbContext.Locations
                .AnyAsync(l => l.Code == code &&
                              l.ZoneId == zoneId &&
                              !l.IsDeleted &&
                              (excludeId == null || l.Id != excludeId));
        }
        public async Task<bool> CheckLocationAvailabilityByBarcodeAsync(string barcode)
        {
            var location = await GetByBarcodeAsync(barcode);
            if (location == null) return false;

            int inventoryItemCount = await InventoryCountByLocationIdAsync(location.Id);

            return inventoryItemCount < location.MaxItems ? true : false;
        }

        public async Task<List<LocationDto>> GetLocationsByWarehouseIdAsync(Guid warehouseId) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .Include(l => l.Zone).ThenInclude(z => z.Warehouse)
                .Where(l => l.Zone.WarehouseId == warehouseId && !l.IsDeleted)
                .ToListAsync());

        public async Task<bool> ActivateLocationAsync(Guid id, bool isActive) =>
            await UpdateLocationPropertyAsync(id, l => l.IsActive = isActive);

        public async Task<List<LocationDto>> GetLocationsByZoneIdAsync(Guid zoneId, bool activeOnly = false) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => l.ZoneId == zoneId && (!activeOnly || l.IsActive))
                .ToListAsync());

        public async Task<List<LocationDto>> GetEmptyLocationsAsync(Guid? zoneId = null) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => l.IsEmpty && (zoneId == null || l.ZoneId == zoneId))
                .ToListAsync());

        public async Task<List<LocationDto>> GetLocationsByTypeAsync(LocationType locationType, Guid? zoneId = null) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => l.Type == locationType && (zoneId == null || l.ZoneId == zoneId))
                .ToListAsync());

        public async Task<int> GetLocationCountByZoneAsync(Guid zoneId) =>
            await _dbContext.Locations.CountAsync(l => l.ZoneId == zoneId && !l.IsDeleted && l.IsActive);

        public async Task<int> GetEmptyLocationCountByZoneAsync(Guid zoneId) =>
            await _dbContext.Locations.CountAsync(l => l.ZoneId == zoneId && l.IsEmpty && l.IsActive && !l.IsDeleted);

        public async Task<bool> MarkLocationAsEmptyAsync(Guid locationId)
        {
            var location = await GetByIdAsync(locationId);
            if (location == null) return false;

            return await UpdateLocationStatusByIdAsync(location, AppConsts.LocationGridStatus.AVAILABLE);
        }

        public async Task<bool> MarkLocationAsOccupiedAsync(Guid locationId)
        {
            var location = await GetByIdAsync(locationId);
            if (location == null) return false;

            return await UpdateLocationStatusByIdAsync(location, AppConsts.LocationGridStatus.OCCUPIED);
        }
        public async Task<bool> MarkLocationAsEmptyByBarcodeAsync(string barcode)
        {
            var location = await GetByBarcodeAsync(barcode);
            if (location == null) return false;

            return await UpdateLocationStatusByIdAsync(location, AppConsts.LocationGridStatus.AVAILABLE);
        }
        public async Task<bool> MarkLocationAsOccupiedByBarcodeAsync(string barcode)
        {
            var location = await GetByBarcodeAsync(barcode);
            if (location == null) return false;

            return await UpdateLocationStatusByIdAsync(location, AppConsts.LocationGridStatus.OCCUPIED);
        }

        public async Task<decimal> GetLocationUtilizationAsync(Guid locationId) =>
            await CalculateLocationUtilizationAsync(locationId);

        public async Task<List<LocationDto>> GetLocationsByAccessTypeAsync(AccessType accessType, Guid? zoneId = null) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => l.AccessType == accessType && (zoneId == null || l.ZoneId == zoneId))
                .ToListAsync());

        public async Task<bool> BulkUpdateLocationStatusAsync(List<Guid> locationIds, bool isActive)
        {
            var locations = await _dbContext.Locations
                .Where(l => locationIds.Contains(l.Id))
                .ToListAsync();

            foreach (var location in locations)
            {
                location.IsActive = isActive;
                location.ModifiedBy = _currentUserService.UserId;
                location.ModifiedAt = _dateTime.Now;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<LocationDto>> SearchLocationsAsync(string searchTerm, Guid? zoneId = null, LocationType? type = null, bool? isEmpty = null) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => (string.IsNullOrEmpty(searchTerm) || l.Name.Contains(searchTerm) || l.Code.Contains(searchTerm)) &&
                           (zoneId == null || l.ZoneId == zoneId) &&
                           (type == null || l.Type == type) &&
                           (isEmpty == null || l.IsEmpty == isEmpty))
                .ToListAsync());

        public async Task<List<LocationDto>> GetLocationsByRowAsync(Guid zoneId, string row) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => l.ZoneId == zoneId && l.Row == row)
                .ToListAsync());

        public async Task<List<LocationDto>> GetLocationsByBayAsync(Guid zoneId, int bay) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => l.ZoneId == zoneId && l.Bay == bay)
                .ToListAsync());

        public async Task<List<LocationDto>> GetLocationsByLevelAsync(Guid zoneId, int level) =>
            _mapper.Map<List<LocationDto>>(await _dbContext.Locations
                .ApplyTenantFilter(_currentUserService)
                .Where(l => l.ZoneId == zoneId && l.Level == level)
                .ToListAsync());

        public async Task<Dictionary<string, int>> GetLocationCountByStatusAsync(Guid? zoneId = null)
        {
            var query = _dbContext.Locations.ApplyTenantFilter(_currentUserService);
            if (zoneId.HasValue) query = query.Where(l => l.ZoneId == zoneId);

            return new Dictionary<string, int>
            {
                ["Active"] = await query.CountAsync(l => l.IsActive),
                ["Inactive"] = await query.CountAsync(l => !l.IsActive),
                ["Empty"] = await query.CountAsync(l => l.IsEmpty),
                ["Occupied"] = await query.CountAsync(l => !l.IsEmpty)
            };
        }

        public async Task<Dictionary<LocationType, int>> GetLocationCountByTypeAsync(Guid? zoneId = null)
        {
            var query = _dbContext.Locations.ApplyTenantFilter(_currentUserService);
            if (zoneId.HasValue) query = query.Where(l => l.ZoneId == zoneId);

            return await query
                .GroupBy(l => l.Type)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<List<LocationDto>> GetTopUtilizedLocationsAsync(int count = 10, Guid? zoneId = null)
        {
            var query = _dbContext.Locations.ApplyTenantFilter(_currentUserService);
            if (zoneId.HasValue) query = query.Where(l => l.ZoneId == zoneId);

            var locations = await query
                .Where(l => !l.IsEmpty)
                .OrderByDescending(l => l.Inventories.Count)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<List<LocationDto>>(locations);
        }

        private async Task<bool> UpdateLocationStatusByIdAsync(Location location, string newStatus)
        {
            var inventoryCount = await InventoryCountByLocationIdAsync(location.Id);
            bool isUpdate = false;

            if (newStatus == AppConsts.LocationGridStatus.AVAILABLE)
            {
                if (!location.IsEmpty && inventoryCount == 0)
                {
                    location.IsEmpty = true;
                    isUpdate = true;
                }
            }
            else if (newStatus == AppConsts.LocationGridStatus.OCCUPIED)
            {
                if (location.IsEmpty && inventoryCount > 0)
                {
                    location.IsEmpty = false;
                    isUpdate = true;
                }
            }
            else
            {
                return false;
            }

            if (isUpdate)
            {
                await _dbContext.SaveChangesAsync();

                await _apiService.NotifyLocationUpdateAsync(location.ZoneId, location.Code);

                return true;
            }
            else
                return false;
        }
        private async Task<bool> UpdateLocationPropertyAsync(Guid locationId, System.Action<Location> updateAction)
        {
            var location = await GetByIdAsync(locationId);
            if (location == null) return false;

            updateAction(location);

            await _dbContext.SaveChangesAsync();

            return true;
        }
        private async Task<bool> UpdateLocationByBarcodePropertyAsync(string barcode, System.Action<Location> updateAction)
        {
            var location = await GetByBarcodeAsync(barcode);

            if (location == null) return false;

            updateAction(location);

            await _dbContext.SaveChangesAsync();

            return true;
        }
        #region Import/Export Methods

        /// <summary>
        /// Generate Excel template for location import
        /// </summary>
        public byte[] GenerateLocationTemplate()
        {
            _logger.LogInformation("Generating location import template");

            try
            {
                var file = ExcelHelper.GenerateLocationImportTemplate();

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating location template");
                throw;
            }
        }

        /// <summary>
        /// Export locations to Excel
        /// </summary>
        public async Task<byte[]> ExportLocationsAsync(Guid? zoneId = null)
        {
            _logger.LogInformation("Exporting locations, ZoneId: {ZoneId}", zoneId);

            try
            {
                var query = _dbContext.Locations
                    .ApplyTenantFilter(_currentUserService)
                    .Include(l => l.Zone)
                        .ThenInclude(z => z.Warehouse)
                    .Include(l => l.Inventories)
                    .AsQueryable();

                if (zoneId.HasValue)
                {
                    query = query.Where(l => l.ZoneId == zoneId.Value);
                }

                var locations = await query.OrderBy(l => l.Zone.Name).ThenBy(l => l.Name).ToListAsync();

                var locationDtos = _mapper.Map<List<LocationDto>>(locations);
                locationDtos.ForEach(x =>
                {
                    x.InventoryCount = locations.Where(y => y.Id == x.Id).ToList().Count();
                });

                var excelFile = ExcelHelper.ExportLocations(locationDtos);

                _logger.LogInformation("Exported {Count} locations", locations.Count);
                return excelFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting locations");
                throw;
            }
        }

        /// <summary>
        /// Validate location import file without saving
        /// </summary>
        public async Task<LocationImportValidationResult> ValidateLocationImportAsync(IFormFile file)
        {
            _logger.LogInformation("Validating location import file: {FileName}", file.FileName);

            var result = new LocationImportValidationResult();

            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");

                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    result.Errors.Add("No worksheet found in the Excel file");
                    return result;
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1)
                {
                    result.Errors.Add("No data rows found in the Excel file");
                    return result;
                }

                result.TotalRows = rowCount - 1; // Exclude header row

                bool isAdmin = _currentUserService.IsInRole(AppConsts.Roles.SYSTEM_ADMIN);

                // Get all warehouses for validation
                var warehouses = await _dbContext.Warehouses
                    .Where(x => x.IsActive && (isAdmin || x.Id == _currentUserService.CurrentWarehouseId))
                    .ToListAsync();

                var warehouseDict = warehouses.ToDictionary(z => z.Code, z => z, StringComparer.OrdinalIgnoreCase);

                // Get all zones for validation
                var zones = await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .ToListAsync();

                var zoneDict = zones.ToDictionary(z => z.Code, z => z, StringComparer.OrdinalIgnoreCase);

                // Validate each row
                for (int row = 2; row <= rowCount; row++)
                {
                    var validationItem = await ValidateLocationRow(worksheet, row, warehouseDict, zoneDict);

                    if (validationItem.IsValid)
                    {
                        result.ValidItems.Add(validationItem);
                    }
                    else
                    {
                        result.Errors.AddRange(validationItem.Errors.Select(e => $"Row {row}: {e}"));
                    }

                    if (validationItem.Warnings.Any())
                    {
                        result.Warnings.AddRange(validationItem.Warnings.Select(w => $"Row {row}: {w}"));
                    }
                }

                result.IsValid = !result.Errors.Any();

                _logger.LogInformation("Validation completed. Valid: {IsValid}, Total: {Total}, Valid Items: {ValidCount}, Errors: {ErrorCount}",
                    result.IsValid, result.TotalRows, result.ValidItems.Count, result.Errors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating location import file");
                result.Errors.Add($"File validation failed: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Import locations from Excel file
        /// </summary>
        public async Task<LocationImportResult> ImportLocationsAsync(IFormFile file)
        {
            _logger.LogInformation("Importing locations from file: {FileName}", file.FileName);

            var result = new LocationImportResult();

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // First validate the file
                var validationResult = await ValidateLocationImportAsync(file);

                result.TotalRows = validationResult.TotalRows;
                result.Errors.AddRange(validationResult.Errors);
                result.Warnings.AddRange(validationResult.Warnings);

                if (!validationResult.IsValid)
                {
                    result.Success = false;
                    return result;
                }

                // Process valid items
                var locationsToCreate = new List<Location>();

                foreach (var validItem in validationResult.ValidItems)
                {
                    try
                    {
                        var location = new Location
                        {
                            Id = Guid.NewGuid(),
                            WarehouseId = validItem.WarehouseId,
                            ZoneId = validItem.ZoneId,
                            Name = validItem.Name.ToUpper(),
                            Code = validItem.Code.ToUpper(),
                            //Type = validItem.Type,
                            //AccessType = validItem.AccessType,
                            Row = validItem.Row?.ToUpper(),
                            Bay = validItem.Bay,
                            Level = validItem.Level,
                            Aisle = validItem.Aisle,
                            Side = validItem.Side,
                            Bin = validItem.Bin,
                            MaxWeight = validItem.MaxWeight,
                            MaxVolume = validItem.MaxVolume,
                            MaxItems = validItem.MaxItems,
                            Length = validItem.Length,
                            Width = validItem.Width,
                            Height = validItem.Height,
                            //PickingPriority = validItem.PickingPriority,
                            //TemperatureZone = validItem.TemperatureZone,
                            Barcode = validItem.Barcode,
                            XCoordinate = validItem.XCoordinate,
                            YCoordinate = validItem.YCoordinate,
                            ZCoordinate = validItem.ZCoordinate,
                            IsActive = validItem.IsActive,
                            IsEmpty = true // New locations are empty by default
                        };

                        // Generate full location code
                        location.FullLocationCode = GenerateFullLocationCode(location);

                        locationsToCreate.Add(location);
                        result.SuccessCount++;
                        result.Results.Add(new LocationImportResultItem
                        {
                            RowNumber = validItem.RowNumber,
                            LocationName = location.Name,
                            LocationCode = location.Code,
                            Status = "Success",
                            Message = "Location created successfully"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing valid location item: {LocationName}", validItem.Name);
                        result.ErrorCount++;
                        result.Errors.Add($"Row {validItem.RowNumber}: Failed to process location - {ex.Message}");
                        result.Results.Add(new LocationImportResultItem
                        {
                            RowNumber = validItem.RowNumber,
                            LocationName = validItem.Name,
                            LocationCode = validItem.Code,
                            Status = "Error",
                            Message = ex.Message
                        });
                    }
                }

                // Bulk insert locations
                if (locationsToCreate.Any())
                {
                    await _dbContext.Locations.AddRangeAsync(locationsToCreate);
                    await _dbContext.SaveChangesAsync();
                }

                result.ProcessedRows = result.SuccessCount + result.ErrorCount;
                result.Success = result.ErrorCount == 0;

                await transaction.CommitAsync();

                _logger.LogInformation("Location import completed. Success: {SuccessCount}, Errors: {ErrorCount}",
                    result.SuccessCount, result.ErrorCount);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error importing locations");
                result.Success = false;
                result.Errors.Add($"Import failed: {ex.Message}");
                result.Results = new List<LocationImportResultItem>();
                result.SuccessCount = 0;
                return result;
            }
        }

        #endregion

        #region Private Helper Methods for Import/Export

        private async Task<LocationImportValidationItem> ValidateLocationRow(ExcelWorksheet worksheet, int row, Dictionary<string, Warehouse> wareHouseDict, Dictionary<string, Zone> zoneDict)
        {
            var item = new LocationImportValidationItem { RowNumber = row };

            try
            {
                // Required fields validation
                var warehouseCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                var zoneCode = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var locationName = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var locationCode = worksheet.Cells[row, 4].Value?.ToString()?.Trim();

                if (string.IsNullOrEmpty(warehouseCode))
                {
                    item.Errors.Add("Warehouse Code is required");
                }
                else if (!wareHouseDict.ContainsKey(warehouseCode))
                {
                    item.Errors.Add($"Warehouse '{warehouseCode}' not found");
                }
                else
                {
                    item.WarehouseId = wareHouseDict[warehouseCode].Id;
                    item.WarehouseCode = warehouseCode;
                }

                if (string.IsNullOrEmpty(zoneCode))
                {
                    item.Errors.Add("Zone Code is required");
                }
                else if (!zoneDict.ContainsKey(zoneCode))
                {
                    item.Errors.Add($"Zone '{zoneCode}' not found");
                }
                else
                {
                    item.ZoneId = zoneDict[zoneCode].Id;
                    item.ZoneCode = zoneCode;
                }

                if (string.IsNullOrEmpty(locationName))
                {
                    item.Errors.Add("Location Name is required");
                }
                else
                {
                    item.Name = locationName;

                    // Check if location name already exists in this zone
                    if (item.ZoneId != Guid.Empty)
                    {
                        if (await LocationNameExistsInZoneAsync(locationName, item.ZoneId))
                        {
                            item.Errors.Add($"Location name '{locationName}' already exists in zone '{zoneCode}'");
                        }
                    }
                }

                if (string.IsNullOrEmpty(locationCode))
                {
                    item.Errors.Add("Location Code is required");
                }
                else
                {
                    item.Code = locationCode;

                    // Check if location code already exists in this zone
                    if (item.ZoneId != Guid.Empty)
                    {
                        if (await LocationCodeExistsInZoneAsync(locationCode, item.ZoneId))
                        {
                            item.Errors.Add($"Location code '{locationCode}' already exists in zone '{zoneCode}'");
                        }
                    }
                }

                // Location type validation
                //if (string.IsNullOrEmpty(locationTypeStr))
                //{
                //    item.Type = LocationType.Floor; // Default
                //    item.Warnings.Add("Location Type not specified, using default 'Floor'");
                //}
                //else if (!Enum.TryParse<LocationType>(locationTypeStr, true, out var locationType))
                //{
                //    item.Errors.Add($"Invalid Location Type '{locationTypeStr}'. Valid values: {string.Join(", ", Enum.GetNames<LocationType>())}");
                //}
                //else
                //{
                //    item.Type = locationType;
                //}

                //// Access type validation
                //var accessTypeStr = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                //if (string.IsNullOrEmpty(accessTypeStr))
                //{
                //    item.AccessType = AccessType.Manual; // Default
                //    item.Warnings.Add("Access Type not specified, using default 'Manual'");
                //}
                //else if (!Enum.TryParse<AccessType>(accessTypeStr, true, out var accessType))
                //{
                //    item.Errors.Add($"Invalid Access Type '{accessTypeStr}'. Valid values: {string.Join(", ", Enum.GetNames<AccessType>())}");
                //}
                //else
                //{
                //    item.AccessType = accessType;
                //}

                // Optional fields
                item.Row = worksheet.Cells[row, 5].Value?.ToString()?.Trim();

                if (int.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out var bay))
                    item.Bay = bay;

                if (int.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out var level))
                    item.Level = level;

                item.Aisle = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                item.Side = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
                item.Bin = worksheet.Cells[row, 10].Value?.ToString()?.Trim();

                // Numeric fields with validation
                if (decimal.TryParse(worksheet.Cells[row, 11].Value?.ToString(), out var maxWeight))
                    item.MaxWeight = maxWeight;

                if (decimal.TryParse(worksheet.Cells[row, 12].Value?.ToString(), out var maxVolume))
                    item.MaxVolume = maxVolume;

                if (int.TryParse(worksheet.Cells[row, 13].Value?.ToString(), out var maxItems))
                    item.MaxItems = maxItems;

                if (decimal.TryParse(worksheet.Cells[row, 14].Value?.ToString(), out var length))
                    item.Length = length;

                if (decimal.TryParse(worksheet.Cells[row, 15].Value?.ToString(), out var width))
                    item.Width = width;

                if (decimal.TryParse(worksheet.Cells[row, 16].Value?.ToString(), out var height))
                    item.Height = height;

                //if (int.TryParse(worksheet.Cells[row, 19].Value?.ToString(), out var pickingPriority))
                //    item.PickingPriority = pickingPriority;

                //item.TemperatureZone = worksheet.Cells[row, 20].Value?.ToString()?.Trim();
                var barcode = worksheet.Cells[row, 17].Value?.ToString()?.Trim();
                item.Barcode = barcode;
                if (!string.IsNullOrEmpty(barcode))
                {
                    // Check if location name already exists in this zone
                    if (item.ZoneId != Guid.Empty)
                    {
                        if (await LocationBarcodeExistsAsync(barcode))
                        {
                            item.Errors.Add($"Location barcode '{barcode}' already exists");
                        }
                    }
                }

                if (decimal.TryParse(worksheet.Cells[row, 18].Value?.ToString(), out var xCoord))
                    item.XCoordinate = xCoord;

                if (decimal.TryParse(worksheet.Cells[row, 19].Value?.ToString(), out var yCoord))
                    item.YCoordinate = yCoord;

                if (decimal.TryParse(worksheet.Cells[row, 20].Value?.ToString(), out var zCoord))
                    item.ZCoordinate = zCoord;

                // Active status
                var isActiveStr = worksheet.Cells[row, 21].Value?.ToString()?.Trim();
                item.IsActive = string.IsNullOrEmpty(isActiveStr) ||
                               isActiveStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                               isActiveStr.Equals("1", StringComparison.OrdinalIgnoreCase);

                // Check position uniqueness if provided
                if (!string.IsNullOrEmpty(item.Row) && item.Bay.HasValue && item.Level.HasValue && item.ZoneId != Guid.Empty)
                {
                    var existingPosition = await _dbContext.Locations
                        .FirstOrDefaultAsync(l => l.ZoneId == item.ZoneId &&
                                                 l.Row == item.Row &&
                                                 l.Bay == item.Bay &&
                                                 l.Level == item.Level &&
                                                 !l.IsDeleted);
                    if (existingPosition != null)
                    {
                        item.Errors.Add($"Position Row {item.Row}, Bay {item.Bay}, Level {item.Level} already exists in zone '{zoneCode}'");
                    }
                }

                item.IsValid = !item.Errors.Any();
            }
            catch (Exception ex)
            {
                item.Errors.Add($"Error processing row: {ex.Message}");
                item.IsValid = false;
            }

            return item;
        }

        #endregion

        #region Location Grid Modal UI link inventory

        /// <summary>
        /// Get available items that can be linked to a location
        /// </summary>
        public async Task<GetLinkableItemsResponseDto> GetAvailableLinkableItemsAsync(GetLinkableItemsRequestDto request)
        {
            _logger.LogDebug("Getting available linkable items for location {LocationId}", request.LocationId);

            try
            {
                // Verify location exists and get current capacity
                var location = await _dbContext.Locations
                    .Include(l => l.Zone)
                    .FirstOrDefaultAsync(l => l.Id == request.LocationId && !l.IsDeleted);

                if (location == null)
                {
                    throw new KeyNotFoundException($"Location with ID {request.LocationId} not found");
                }

                // Get current item count in location
                var currentItemCount = await GetCurrentLocationItemCountAsync(request.LocationId);

                // Get available clients for filter
                var availableClients = await GetAvailableClientsForLinkingAsync();

                var availableCapacity = Math.Max(0, location.MaxItems - currentItemCount);

                List<LinkableInventoryItemDto> availableItems = new List<LinkableInventoryItemDto>();
                if (availableCapacity > 0 || location.Zone.Name != AppConsts.ZoneName.RACKING)
                {
                    // Get available items based on filters
                    request.ZoneName = location.Zone!.Name;
                    request.Location = location;
                    availableItems.AddRange(await GetFilteredLinkableItemsAsync(request));
                }

                var response = new GetLinkableItemsResponseDto
                {
                    AvailableItems = availableItems,
                    CurrentLocationItemCount = currentItemCount,
                    MaxItems = location.MaxItems,
                    AvailableCapacity = availableCapacity,
                    AvailableClients = availableClients
                };

                _logger.LogDebug("Found {ItemCount} available items for location {LocationId}",
                    availableItems.Count, request.LocationId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available linkable items for location {LocationId}", request.LocationId);
                throw;
            }
        }

        /// <summary>
        /// Link selected items to a location
        /// </summary>
        public async Task<LinkItemsToLocationResponseDto> LinkItemsToLocationAsync(LinkItemsToLocationRequestDto request)
        {
            _logger.LogInformation("Linking {ItemCount} items to location {LocationId}",
                request.Items.Count, request.LocationId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Verify location exists and get current capacity
                var location = await _dbContext.Locations
                    .FirstOrDefaultAsync(l => l.Id == request.LocationId && !l.IsDeleted);

                if (location == null)
                {
                    return new LinkItemsToLocationResponseDto
                    {
                        Success = false,
                        Message = $"Location not found"
                    };
                }

                // Get current item count
                var currentItemCount = await GetCurrentLocationItemCountAsync(request.LocationId);

                // Validate capacity
                var requestedItemCount = request.Items.Count;
                //maxItems == 0 which mean got unlimited items, so only checking for maxitems != 0
                if (currentItemCount + requestedItemCount > location.MaxItems && location.MaxItems != 0)
                {
                    return new LinkItemsToLocationResponseDto
                    {
                        Success = false,
                        Message = $"Cannot link {requestedItemCount} items. Location capacity: {location.MaxItems}, Current items: {currentItemCount}, Available capacity: {location.MaxItems - currentItemCount}"
                    };
                }

                // Verify all items are still available (not already linked)
                //var unavailableItems = await ValidateItemsAvailabilityAsync(request.Items);
                //if (unavailableItems.Any())
                //{
                //    return new LinkItemsToLocationResponseDto
                //    {
                //        Success = false,
                //        Message = $"Some items are no longer available for linking"
                //    };
                //}

                // Perform the linking
                var linkedCount = 0;
                List<Guid> previousLocationIds = new List<Guid>();
                foreach (var item in request.Items)
                {
                    switch (item.ItemType)
                    {
                        case LinkableItemType.Inventory:
                            var inventory = await _dbContext.Inventories.FindAsync(item.ItemId);
                            if (inventory != null)
                            {
                                if (inventory.LocationId != Guid.Empty)
                                {
                                    previousLocationIds.Add(inventory.LocationId);
                                }
                                InventoryMovement inventoryMovement = ConvertToInventoryMovement(inventory.Id, nameof(Inventory), 
                                    inventory.LocationId, request.LocationId, inventory.Quantity, MovementType.Transfer, inventory.LotNumber);

                                _dbContext.InventoryMovements.Add(inventoryMovement);

                                inventory.LocationId = request.LocationId;
                                linkedCount++;
                            }
                            break;

                        case LinkableItemType.GIV_FG_Pallet:
                            var fgPallet = await _dbContext.GIV_FG_ReceivePallets.FindAsync(item.ItemId);
                            if (fgPallet != null)
                            {
                                if (fgPallet.LocationId != null)
                                {
                                    previousLocationIds.Add(fgPallet.LocationId.Value);
                                }
                                InventoryMovement inventoryMovement = ConvertToInventoryMovement(fgPallet.Id, nameof(GIV_FG_ReceivePallet),
                                    fgPallet.LocationId, request.LocationId, 0, MovementType.Transfer, fgPallet.PalletCode ?? string.Empty);

                                _dbContext.InventoryMovements.Add(inventoryMovement);

                                fgPallet.LocationId = request.LocationId;
                                fgPallet.StoredBy = _currentUserService.GetCurrentUsername;
                                linkedCount++;
                            }
                            break;

                        case LinkableItemType.GIV_RM_Pallet:
                            var rmPallet = await _dbContext.GIV_RM_ReceivePallets.FindAsync(item.ItemId);
                            if (rmPallet != null)
                            {
                                if (rmPallet.LocationId != null)
                                {
                                    previousLocationIds.Add(rmPallet.LocationId.Value);
                                }
                                InventoryMovement inventoryMovement = ConvertToInventoryMovement(rmPallet.Id, nameof(GIV_RM_ReceivePallet),
                                    rmPallet.LocationId, request.LocationId, 0, MovementType.Transfer, rmPallet.PalletCode ?? string.Empty);

                                _dbContext.InventoryMovements.Add(inventoryMovement);

                                rmPallet.LocationId = request.LocationId;
                                rmPallet.StoredBy = _currentUserService.GetCurrentUsername;
                                linkedCount++;
                            }
                            break;
                    }
                }

                await _dbContext.SaveChangesAsync();

                // Update location status
                var newItemCount = currentItemCount + linkedCount;
                await UpdateLocationStatusAfterLinking(location, newItemCount);

                await transaction.CommitAsync();

                // Trigger SignalR update
                //await _apiService.NotifyLocationUpdateAsync(location.ZoneId, location.Code);

                var newStatus = GetLocationStatusString(newItemCount, location.MaxItems);

                _logger.LogInformation("Successfully linked {LinkedCount} items to location {LocationId}",
                    linkedCount, request.LocationId);

                return new LinkItemsToLocationResponseDto
                {
                    Success = true,
                    Message = $"Successfully linked {linkedCount} items to location",
                    LinkedItemsCount = linkedCount,
                    NewLocationItemCount = newItemCount,
                    NewLocationStatus = newStatus,
                    PreviousLocationIds = previousLocationIds
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error linking items to location {LocationId}", request.LocationId);

                return new LinkItemsToLocationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while linking items to location"
                };
            }
        }

        // Helper methods

        private async Task<int> GetCurrentLocationItemCountAsync(Guid locationId)
        {
            var inventoryCount = await _dbContext.Inventories
                .CountAsync(i => i.LocationId == locationId && !i.IsDeleted);

            var fgPalletCount = await _dbContext.GIV_FG_ReceivePallets
                .CountAsync(p => p.LocationId == locationId && !p.IsDeleted);

            var rmPalletCount = await _dbContext.GIV_RM_ReceivePallets
                .CountAsync(p => p.LocationId == locationId && !p.IsDeleted);

            return inventoryCount + fgPalletCount + rmPalletCount;
        }
        private InventoryMovement ConvertToInventoryMovement(Guid entityId, string entityName, Guid? fromLocId, Guid? toLocId, decimal qty, MovementType movType, string? refNo)
        {
            InventoryMovement inventoryMovement = new InventoryMovement();
            inventoryMovement.EntityId = entityId;
            inventoryMovement.EntityName = entityName;
            inventoryMovement.FromLocationId = fromLocId;
            inventoryMovement.ToLocationId = toLocId;
            inventoryMovement.Quantity = qty;
            inventoryMovement.Type = movType;
            inventoryMovement.ReferenceNumber = refNo ?? string.Empty;
            inventoryMovement.PerformedBy = _currentUserService.GetCurrentUsername;
            inventoryMovement.MovementDate = _dateTime.Now;
            inventoryMovement.WarehouseId = _currentUserService.CurrentWarehouseId;

            return inventoryMovement;
        }
        private async Task<List<ClientDropdownDto>> GetAvailableClientsForLinkingAsync()
        {
            // Get clients that have inventory items
            var clientsWithInventory = await _dbContext.Inventories
                .Where(i => i.LocationId == null && !i.IsDeleted)
                .Select(i => new { i.Client.Id, i.Client.Name, i.Client.Code })
                .Distinct()
                .ToListAsync();

            // Add Givaudan client for GIV pallets
            var givaudanClient = await _dbContext.Clients
                .Where(c => c.Code == "GIV")
                .Select(c => new { c.Id, c.Name, c.Code })
                .FirstOrDefaultAsync();

            var clients = new List<ClientDropdownDto>();

            clients.AddRange(clientsWithInventory.Select(c => new ClientDropdownDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code
            }));

            if (givaudanClient != null)
            {
                // Only add if not already in the list
                if (!clients.Any(c => c.Id == givaudanClient.Id))
                {
                    clients.Add(new ClientDropdownDto
                    {
                        Id = givaudanClient.Id,
                        Name = givaudanClient.Name,
                        Code = givaudanClient.Code
                    });
                }
            }

            return clients.OrderBy(c => c.Name).ToList();
        }

        private async Task<List<LinkableInventoryItemDto>> GetFilteredLinkableItemsAsync(GetLinkableItemsRequestDto request)
        {
            // Get allowed zone names based on request
            var allowedZoneNames = request.ZoneName switch
            {
                AppConsts.ZoneName.RACKING => new[] { AppConsts.ZoneName.QUEUE, AppConsts.ZoneName.REFRIGERATED },
                AppConsts.ZoneName.QUEUE => new[] { AppConsts.ZoneName.RACKING, AppConsts.ZoneName.QUEUE, AppConsts.ZoneName.REFRIGERATED },
                AppConsts.ZoneName.REFRIGERATED => new[] { AppConsts.ZoneName.QUEUE, AppConsts.ZoneName.RACKING },
                _ => Array.Empty<string>()
            };

            var items = new List<LinkableInventoryItemDto>();

            // Get Inventory items
            if (!request.ItemType.HasValue || request.ItemType == LinkableItemType.Inventory)
            {
                var inventoryQuery = _dbContext.Inventories
                    .Include(i => i.Product)
                    .Include(i => i.Client)
                    .Include(i => i.Location)
                        .ThenInclude(p => p.Zone)
                    .Where(i => !i.IsDeleted 
                    && (i.LocationId == Guid.Empty || (allowedZoneNames.Contains(i.Location.Zone.Name) && i.LocationId != request.LocationId)));

                // Apply client filter for inventory
                if (request.ClientId.HasValue)
                {
                    inventoryQuery = inventoryQuery.Where(i => i.ClientId == request.ClientId.Value);
                }

                var inventoryItems = await inventoryQuery
                    .Select(i => new LinkableInventoryItemDto
                    {
                        Id = i.Id,
                        Type = LinkableItemType.Inventory,
                        DisplayName = i.Product.Name,
                        SKUCode = i.Product.SKU,
                        ClientName = i.Client.Name,
                        ReceiveDate = i.ReceivedDate,
                        AdditionalInfo = $"Lot: {i.LotNumber}, Serial: {i.SerialNumber}",
                        LocationAndZoneName = i.LocationId == Guid.Empty ? string.Empty : $"{i.Location.Zone.Name} - {i.Location.Barcode}"
                    })
                    .ToListAsync();

                items.AddRange(inventoryItems);
            }

            // Get GIV FG Pallet items
            if (!request.ItemType.HasValue || request.ItemType == LinkableItemType.GIV_FG_Pallet)
            {
                var givaudanClientId = await GetGivaudanClientIdAsync();
                
                // Only show if Givaudan is selected or non selected
                if (!request.ClientId.HasValue || request.ClientId == givaudanClientId)
                {
                    var fgPalletItems = await _dbContext.GIV_FG_ReceivePallets
                        .Include(p => p.Receive)
                            .ThenInclude(r => r.FinishedGood)
                        .Include(p => p.Location)
                            .ThenInclude(r => r.Zone)
                        .Where(p => !p.IsDeleted && p.Receive != null 
                        && (p.LocationId == null || (allowedZoneNames.Contains(p.Location.Zone.Name) && p.LocationId != request.LocationId)))
                        .Select(p => new LinkableInventoryItemDto
                        {
                            Id = p.Id,
                            Type = LinkableItemType.GIV_FG_Pallet,
                            DisplayName = p.Receive.FinishedGood.Description ?? "Unknown FG",
                            SKUCode = p.Receive.FinishedGood.SKU ?? "N/A",
                            ClientName = "Givaudan",
                            ReceiveDate = p.ReceivedDate,
                            AdditionalInfo = $"Pallet: {p.PalletCode}, Batch: {p.Receive.BatchNo}",
                            LocationAndZoneName = p.LocationId == null ? string.Empty : $"{p.Location.Zone.Name} - {p.Location.Barcode}"

                        })
                        .ToListAsync();

                    items.AddRange(fgPalletItems);
                }
            }

            // Get GIV RM Pallet items
            if (!request.ItemType.HasValue || request.ItemType == LinkableItemType.GIV_RM_Pallet)
            {
                var givaudanClientId = await GetGivaudanClientIdAsync();

                // Only show if Givaudan is selected or non selected
                if (!request.ClientId.HasValue || request.ClientId == givaudanClientId)
                {
                    var rmPalletItems = await _dbContext.GIV_RM_ReceivePallets
                        .Include(p => p.GIV_RM_Receive)
                            .ThenInclude(r => r.RawMaterial)
                        .Where(p => !p.IsDeleted && p.GIV_RM_Receive != null 
                        && (p.LocationId == null || (allowedZoneNames.Contains(p.Location.Zone.Name) && p.LocationId != request.LocationId)))
                        .Select(p => new LinkableInventoryItemDto
                        {
                            Id = p.Id,
                            Type = LinkableItemType.GIV_RM_Pallet,
                            DisplayName = p.GIV_RM_Receive.RawMaterial.Description ?? "Unknown RM",
                            SKUCode = p.GIV_RM_Receive.RawMaterial.MaterialNo,
                            ClientName = "Givaudan",
                            ReceiveDate = p.GIV_RM_Receive.ReceivedDate,
                            AdditionalInfo = $"Pallet: {p.PalletCode}, Batch: {p.GIV_RM_Receive.BatchNo}",
                            LocationAndZoneName = p.LocationId == null ? string.Empty : $"{p.Location.Zone.Name} - {p.Location.Barcode}"
                        })
                        .ToListAsync();

                    items.AddRange(rmPalletItems);
                }
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                items = items.Where(i =>
                    i.DisplayName.ToLower().Contains(searchTerm) ||
                    i.SKUCode.ToLower().Contains(searchTerm) ||
                    i.ClientName.ToLower().Contains(searchTerm) ||
                    (i.AdditionalInfo?.ToLower().Contains(searchTerm) ?? false)
                ).ToList();
            }

            return items.OrderBy(i => i.Type).ThenBy(i => i.DisplayName).ToList();
        }

        private async Task<Guid?> GetGivaudanClientIdAsync()
        {
            var client = await _dbContext.Clients
                .Where(c => c.Code == "GIV")
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            return client;
        }

        private async Task<List<string>> ValidateItemsAvailabilityAsync(List<LinkableItemRequestDto> items)
        {
            var unavailableItems = new List<string>();

            foreach (var item in items)
            {
                bool isAvailable = item.ItemType switch
                {
                    LinkableItemType.Inventory => await _dbContext.Inventories
                        .AnyAsync(i => i.Id == item.ItemId && i.LocationId == null && !i.IsDeleted),
                    LinkableItemType.GIV_FG_Pallet => await _dbContext.GIV_FG_ReceivePallets
                        .AnyAsync(p => p.Id == item.ItemId && p.LocationId == null && !p.IsDeleted),
                    LinkableItemType.GIV_RM_Pallet => await _dbContext.GIV_RM_ReceivePallets
                        .AnyAsync(p => p.Id == item.ItemId && p.LocationId == null && !p.IsDeleted),
                    _ => false
                };

                if (!isAvailable)
                {
                    unavailableItems.Add($"{item.ItemType}:{item.ItemId}");
                }
            }

            return unavailableItems;
        }

        private async Task UpdateLocationStatusAfterLinking(Location location, int newItemCount)
        {
            var wasEmpty = location.IsEmpty;
            var newIsEmpty = newItemCount == 0;

            if (wasEmpty != newIsEmpty)
            {
                location.IsEmpty = newIsEmpty;
                await _dbContext.SaveChangesAsync();
            }
        }

        private string GetLocationStatusString(int currentItems, int maxItems)
        {
            if (currentItems == 0)
                return AppConsts.LocationGridStatus.AVAILABLE;
            else if (currentItems >= maxItems)
                return AppConsts.LocationGridStatus.OCCUPIED;
            else
                return AppConsts.LocationGridStatus.PARTIAL;
        }
        #endregion
    }
}