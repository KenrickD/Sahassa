using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS.Application.Extensions;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Zones;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;

namespace WMS.Application.Services
{
    public class ZoneService : IZoneService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly ILogger<ZoneService> _logger;

        public ZoneService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IDateTime dateTime,
            ILogger<ZoneService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _logger = logger;
        }

        public async Task<PaginatedResult<ZoneDto>> GetPaginatedZones(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated zones: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                searchTerm, skip, take, sortColumn, sortAscending);

            try
            {
                // Start with base query using tenant filter
                var query = _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .Include(z => z.Warehouse)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogDebug("Applying search filter: {SearchTerm}", searchTerm);

                    query = query.Where(z =>
                        z.Name.ToLower().Contains(searchTerm) ||
                        z.Code.ToLower().Contains(searchTerm) ||
                        (z.Description != null && z.Description.ToLower().Contains(searchTerm)) ||
                        (z.Warehouse != null && z.Warehouse.Name.ToLower().Contains(searchTerm))
                    );
                }

                // Apply sorting
                query = ApplySorting(query, sortColumn, !sortAscending);

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total zones matching criteria: {TotalCount}", totalCount);

                // Apply pagination
                var zones = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var zoneDtos = _mapper.Map<List<ZoneDto>>(zones);

                // Get location counts for each zone
                foreach (var zoneDto in zoneDtos)
                {
                    zoneDto.LocationCount = await _dbContext.Locations
                        .CountAsync(l => l.ZoneId == zoneDto.Id && !l.IsDeleted);
                }

                _logger.LogInformation("Retrieved {ZoneCount} paginated zones (skip={Skip}, take={Take}) from total of {TotalCount}",
                    zoneDtos.Count, skip, take, totalCount);

                return new PaginatedResult<ZoneDto>
                {
                    Items = zoneDtos,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated zones: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private IQueryable<Zone> ApplySorting(IQueryable<Zone> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(z => z.Warehouse.Name) : query.OrderBy(z => z.Warehouse.Name),
                2 => sortDescending ? query.OrderByDescending(z => z.Name) : query.OrderBy(z => z.Name),
                3 => sortDescending ? query.OrderByDescending(z => z.Code) : query.OrderBy(z => z.Code),
                4 => sortDescending ? query.OrderByDescending(z => z.Description) : query.OrderBy(z => z.Description),
                5 => sortDescending ? query.OrderByDescending(z => z.IsActive) : query.OrderBy(z => z.IsActive),
                6 => sortDescending ? query.OrderByDescending(z => z.CreatedAt) : query.OrderBy(z => z.CreatedAt),
                _ => sortDescending ? query.OrderByDescending(z => z.Warehouse.Name) : query.OrderBy(z => z.Warehouse.Name)
            };
        }

        public async Task<List<ZoneDto>> GetAllZonesAsync()
        {
            _logger.LogDebug("Getting all zones");

            try
            {
                var zones = await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .Include(z => z.Warehouse)
                    .ToListAsync();

                var result = _mapper.Map<List<ZoneDto>>(zones);

                // Get location counts
                foreach (var zoneDto in result)
                {
                    zoneDto.LocationCount = await _dbContext.Locations
                        .CountAsync(l => l.ZoneId == zoneDto.Id && !l.IsDeleted);
                }

                _logger.LogInformation("Retrieved {ZoneCount} zones", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all zones");
                throw;
            }
        }

        public async Task<List<ZoneDto>> GetZonesByWarehouseIdAsync(Guid warehouseId)
        {
            _logger.LogDebug("Getting zones for warehouse: {WarehouseId}", warehouseId);

            try
            {
                var zones = await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .Include(z => z.Warehouse)
                    .Where(z => z.WarehouseId == warehouseId)
                    .ToListAsync();

                var result = _mapper.Map<List<ZoneDto>>(zones);

                // Get location counts
                foreach (var zoneDto in result)
                {
                    zoneDto.LocationCount = await _dbContext.Locations
                        .CountAsync(l => l.ZoneId == zoneDto.Id && !l.IsDeleted);
                }

                _logger.LogInformation("Retrieved {ZoneCount} zones for warehouse {WarehouseId}",
                    result.Count, warehouseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving zones for warehouse {WarehouseId}", warehouseId);
                throw;
            }
        }

        public async Task<ZoneDto> GetZoneByIdAsync(Guid id)
        {
            _logger.LogDebug("Getting zone by ID: {ZoneId}", id);

            try
            {
                var zone = await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .Include(z => z.Warehouse)
                    .FirstOrDefaultAsync(z => z.Id == id);

                if (zone == null)
                {
                    _logger.LogWarning("Zone not found: {ZoneId}", id);
                    throw new KeyNotFoundException($"Zone with ID {id} not found.");
                }

                var result = _mapper.Map<ZoneDto>(zone);

                // Get location count
                result.LocationCount = await _dbContext.Locations
                    .CountAsync(l => l.ZoneId == id && !l.IsDeleted);

                _logger.LogDebug("Successfully retrieved zone: {ZoneId} - {ZoneName}", id, result.Name);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving zone {ZoneId}", id);
                throw;
            }
        }

        public async Task<ZoneDto> CreateZoneAsync(ZoneCreateDto zoneDto)
        {
            _logger.LogInformation("Creating new zone: {ZoneName}", zoneDto.Name);

            // Validate unique constraints
            if (await ZoneNameExistsInWarehouseAsync(zoneDto.Name, zoneDto.WarehouseId))
            {
                var message = $"Zone name '{zoneDto.Name}' already exists in this warehouse";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (await ZoneCodeExistsInWarehouseAsync(zoneDto.Code, zoneDto.WarehouseId))
            {
                var message = $"Zone code '{zoneDto.Code}' already exists in this warehouse";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var zone = _mapper.Map<Zone>(zoneDto);
                zone.Id = Guid.NewGuid();
                zone.CreatedBy = _currentUserService.UserId;
                zone.CreatedAt = _dateTime.Now;

                await _dbContext.AddAsync(zone);
                await _dbContext.SaveChangesAsync();

                // Reload zone with warehouse information
                await _dbContext.Entry(zone).Reference(z => z.Warehouse).LoadAsync();

                await transaction.CommitAsync();

                var result = _mapper.Map<ZoneDto>(zone);
                result.LocationCount = 0; // New zone has no locations

                _logger.LogInformation("Zone created successfully: {ZoneId} - {ZoneName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating zone: {ZoneName}", zoneDto.Name);
                throw;
            }
        }

        public async Task<ZoneDto> UpdateZoneAsync(Guid id, ZoneUpdateDto zoneDto)
        {
            _logger.LogInformation("Updating zone: {ZoneId} - {ZoneName}", id, zoneDto.Name);

            // Validate unique constraints
            if (await ZoneNameExistsAsync(zoneDto.Name, id))
            {
                var message = $"Zone name '{zoneDto.Name}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (await ZoneCodeExistsAsync(zoneDto.Code, id))
            {
                var message = $"Zone code '{zoneDto.Code}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var zone = await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .Include(z => z.Warehouse)
                    .FirstOrDefaultAsync(z => z.Id == id);

                if (zone == null)
                {
                    var message = $"Zone with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Update zone properties
                _mapper.Map(zoneDto, zone);
                zone.ModifiedBy = _currentUserService.UserId;
                zone.ModifiedAt = _dateTime.Now;

                _dbContext.Update(zone);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = _mapper.Map<ZoneDto>(zone);

                // Get location count
                result.LocationCount = await _dbContext.Locations
                    .CountAsync(l => l.ZoneId == id && !l.IsDeleted);

                _logger.LogInformation("Zone updated successfully: {ZoneId} - {ZoneName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating zone: {ZoneId} - {ZoneName}", id, zoneDto.Name);
                throw;
            }
        }

        public async Task<bool> DeleteZoneAsync(Guid id)
        {
            _logger.LogInformation("Deleting zone: {ZoneId}", id);

            try
            {
                var zone = await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .FirstOrDefaultAsync(z => z.Id == id);

                if (zone == null)
                {
                    var message = $"Zone with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Check if zone has associated locations
                var hasLocations = await _dbContext.Locations.AnyAsync(l => l.ZoneId == id && !l.IsDeleted);

                if (hasLocations)
                {
                    var message = $"Cannot delete zone '{zone.Name}' as it has associated locations";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                zone.IsDeleted = true;
                zone.ModifiedBy = _currentUserService.UserId;
                zone.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Zone deleted successfully: {ZoneId} - {ZoneName}",
                    id, zone.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting zone: {ZoneId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateZoneAsync(Guid id, bool isActive)
        {
            _logger.LogInformation("Changing zone status: {ZoneId} to {IsActive}", id, isActive);

            try
            {
                var zone = await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .FirstOrDefaultAsync(z => z.Id == id);

                if (zone == null)
                {
                    var message = $"Zone with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                zone.IsActive = isActive;
                zone.ModifiedBy = _currentUserService.UserId;
                zone.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Zone status changed successfully: {ZoneId} - {ZoneName} to {IsActive}",
                    id, zone.Name, isActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing zone status: {ZoneId} to {IsActive}", id, isActive);
                throw;
            }
        }

        public async Task<bool> ZoneExistsAsync(Guid id)
        {
            try
            {
                return await _dbContext.Zones
                    .ApplyTenantFilter(_currentUserService)
                    .AnyAsync(z => z.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if zone exists: {ZoneId}", id);
                throw;
            }
        }

        public async Task<bool> ZoneNameExistsInWarehouseAsync(string name, Guid warehouseId, Guid? excludeId = null)
        {
            return await _dbContext.Zones
                .AnyAsync(z => z.Name == name &&
                              z.WarehouseId == warehouseId &&
                              !z.IsDeleted &&
                              (excludeId == null || z.Id != excludeId));
        }

        public async Task<bool> ZoneCodeExistsInWarehouseAsync(string code, Guid warehouseId, Guid? excludeId = null)
        {
            return await _dbContext.Zones
                .AnyAsync(z => z.Code == code &&
                              z.WarehouseId == warehouseId &&
                              !z.IsDeleted &&
                              (excludeId == null || z.Id != excludeId));
        }

        public async Task<List<ZoneDto>> GetZonesByWarehouseIdAsync(Guid warehouseId, bool activeOnly = false)
        {
            var query = _dbContext.Zones
                .Include(z => z.Warehouse)
                .Where(z => z.WarehouseId == warehouseId && !z.IsDeleted);

            if (activeOnly)
            {
                query = query.Where(z => z.IsActive);
            }

            var zones = await query.ToListAsync();
            var result = _mapper.Map<List<ZoneDto>>(zones);

            // Get location counts
            foreach (var zoneDto in result)
            {
                zoneDto.LocationCount = await _dbContext.Locations
                    .CountAsync(l => l.ZoneId == zoneDto.Id && !l.IsDeleted);
            }

            return result;
        }

        public async Task<int> GetZoneCountByWarehouseAsync(Guid warehouseId)
        {
            return await _dbContext.Zones
                .CountAsync(z => z.WarehouseId == warehouseId && !z.IsDeleted && z.IsActive);
        }

        // Update the existing ZoneNameExistsAsync and ZoneCodeExistsAsync methods to be warehouse-aware
        public async Task<bool> ZoneNameExistsAsync(string name, Guid? excludeId = null)
        {
            // Get current user's warehouse context
            var warehouseId = _currentUserService.CurrentWarehouseId;

            return await _dbContext.Zones
                .AnyAsync(z => z.Name == name &&
                              z.WarehouseId == warehouseId &&
                              !z.IsDeleted &&
                              (excludeId == null || z.Id != excludeId));
        }

        public async Task<bool> ZoneCodeExistsAsync(string code, Guid? excludeId = null)
        {
            // Get current user's warehouse context
            var warehouseId = _currentUserService.CurrentWarehouseId;

            return await _dbContext.Zones
                .AnyAsync(z => z.Code == code &&
                              z.WarehouseId == warehouseId &&
                              !z.IsDeleted &&
                              (excludeId == null || z.Id != excludeId));
        }
    }
}