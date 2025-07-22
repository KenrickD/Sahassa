using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Warehouses;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;

namespace WMS.Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly ILogger<WarehouseService> _logger;

        public WarehouseService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IDateTime dateTime,
            ILogger<WarehouseService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _logger = logger;
        }

        public async Task<PaginatedResult<WarehouseListItemDto>> GetPaginatedWarehousesAsync(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated warehouses: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                searchTerm, skip, take, sortColumn, sortAscending);

            try
            {
                var query = _dbContext.Warehouses
                    .Where(w => !w.IsDeleted)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogDebug("Applying search filter: {SearchTerm}", searchTerm);

                    query = query.Where(w =>
                        w.Name.ToLower().Contains(searchTerm) ||
                        w.Code.ToLower().Contains(searchTerm) ||
                        (w.City != null && w.City.ToLower().Contains(searchTerm)) ||
                        (w.State != null && w.State.ToLower().Contains(searchTerm)) ||
                        (w.Country != null && w.Country.ToLower().Contains(searchTerm)) ||
                        (w.ContactPerson != null && w.ContactPerson.ToLower().Contains(searchTerm))
                    );
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total warehouses matching criteria: {TotalCount}", totalCount);

                // Apply sorting
                query = ApplySorting(query, sortColumn, !sortAscending);

                // Apply pagination and select required fields
                var warehouses = await query
                    .Skip(skip)
                    .Take(take)
                    .Select(w => new WarehouseListItemDto
                    {
                        Id = w.Id,
                        Name = w.Name,
                        Code = w.Code,
                        City = w.City,
                        State = w.State,
                        Country = w.Country,
                        ContactPerson = w.ContactPerson,
                        ContactEmail = w.ContactEmail,
                        IsActive = w.IsActive,
                        ClientCount = w.Clients.Count(c => !c.IsDeleted),
                        ZoneCount = w.Zones.Count(z => !z.IsDeleted),
                        LocationCount = w.Zones.SelectMany(z => z.Locations).Count(l => !l.IsDeleted),
                        CreatedAt = w.CreatedAt,
                        CreatedBy = w.CreatedBy
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {WarehouseCount} paginated warehouses (skip={Skip}, take={Take}) from total of {TotalCount}",
                    warehouses.Count, skip, take, totalCount);

                return new PaginatedResult<WarehouseListItemDto>
                {
                    Items = warehouses,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated warehouses: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<List<WarehouseDto>> GetAllWarehousesAsync()
        {
            _logger.LogDebug("Getting all warehouses");

            try
            {
                var warehouses = await _dbContext.Warehouses
                    .Include(w => w.Configuration)
                    .Where(w => !w.IsDeleted)
                    .Select(w => new Warehouse
                    {
                        Id = w.Id,
                        Name = w.Name,
                        Code = w.Code,
                        Address = w.Address,
                        City = w.City,
                        State = w.State,
                        Country = w.Country,
                        ZipCode = w.ZipCode,
                        ContactPerson = w.ContactPerson,
                        ContactEmail = w.ContactEmail,
                        ContactPhone = w.ContactPhone,
                        IsActive = w.IsActive,
                        CreatedAt = w.CreatedAt,
                        CreatedBy = w.CreatedBy,
                        ModifiedAt = w.ModifiedAt,
                        ModifiedBy = w.ModifiedBy,
                        Configuration = w.Configuration,
                        Clients = w.Clients.Where(c => !c.IsDeleted).ToList(),
                        Zones = w.Zones.Where(z => !z.IsDeleted).ToList()
                    })
                    .ToListAsync();

                var warehouseDtos = _mapper.Map<List<WarehouseDto>>(warehouses);

                // Calculate location counts
                foreach (var warehouseDto in warehouseDtos)
                {
                    warehouseDto.LocationCount = await _dbContext.Locations
                        .CountAsync(l => l.Zone.WarehouseId == warehouseDto.Id && !l.IsDeleted);

                    warehouseDto.UserCount = await _dbContext.Users
                        .CountAsync(u => u.WarehouseId == warehouseDto.Id && !u.IsDeleted);
                }

                _logger.LogInformation("Retrieved {WarehouseCount} warehouses", warehouseDtos.Count);
                return warehouseDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all warehouses: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<WarehouseDto> GetWarehouseByIdAsync(Guid id)
        {
            _logger.LogDebug("Getting warehouse by ID: {WarehouseId}", id);

            try
            {
                var warehouse = await _dbContext.Warehouses
                    .Include(w => w.Configuration)
                    .Include(w => w.Clients.Where(c => !c.IsDeleted))
                    .Include(w => w.Zones.Where(z => !z.IsDeleted))
                    .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse not found: {WarehouseId}", id);
                    throw new KeyNotFoundException($"Warehouse with ID {id} not found.");
                }

                var warehouseDto = _mapper.Map<WarehouseDto>(warehouse);

                // Calculate additional counts
                warehouseDto.LocationCount = await _dbContext.Locations
                    .CountAsync(l => l.Zone.WarehouseId == id && !l.IsDeleted);

                warehouseDto.UserCount = await _dbContext.Users
                    .CountAsync(u => u.WarehouseId == id && !u.IsDeleted);

                _logger.LogInformation("Retrieved warehouse: {WarehouseName} (ID: {WarehouseId})",
                    warehouse.Name, id);

                return warehouseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving warehouse {WarehouseId}: {ErrorMessage}", id, ex.Message);
                throw;
            }
        }

        public async Task<WarehouseDto> CreateWarehouseAsync(WarehouseCreateDto warehouseDto)
        {
            _logger.LogInformation("Creating warehouse: {WarehouseName} ({WarehouseCode})",
                warehouseDto.Name, warehouseDto.Code);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Check if warehouse name already exists
                if (await WarehouseNameExistsAsync(warehouseDto.Name))
                {
                    _logger.LogWarning("Warehouse creation failed - Name already exists: {WarehouseName}", warehouseDto.Name);
                    throw new InvalidOperationException($"Warehouse with name '{warehouseDto.Name}' already exists.");
                }

                // Check if warehouse code already exists
                if (await WarehouseCodeExistsAsync(warehouseDto.Code))
                {
                    _logger.LogWarning("Warehouse creation failed - Code already exists: {WarehouseCode}", warehouseDto.Code);
                    throw new InvalidOperationException($"Warehouse with code '{warehouseDto.Code}' already exists.");
                }

                var warehouse = _mapper.Map<Warehouse>(warehouseDto);
                warehouse.Id = Guid.NewGuid();
                warehouse.CreatedBy = _currentUserService.UserId;
                warehouse.CreatedAt = _dateTime.Now;

                // Set up warehouse configuration
                warehouse.Configuration = new WarehouseConfiguration
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouse.Id,
                    RequiresLotTracking = warehouseDto.RequiresLotTracking,
                    RequiresExpirationDates = warehouseDto.RequiresExpirationDates,
                    UsesSerialNumbers = warehouseDto.UsesSerialNumbers,
                    AutoAssignLocations = warehouseDto.AutoAssignLocations,
                    InventoryStrategy = warehouseDto.InventoryStrategy,
                    DefaultMeasurementUnit = warehouseDto.DefaultMeasurementUnit,
                    DefaultDaysToExpiry = warehouseDto.DefaultDaysToExpiry,
                    BarcodeFormat = warehouseDto.BarcodeFormat,
                    CompanyLogoUrl = warehouseDto.CompanyLogoUrl,
                    ThemePrimaryColor = warehouseDto.ThemePrimaryColor,
                    ThemeSecondaryColor = warehouseDto.ThemeSecondaryColor,
                    CreatedBy = _currentUserService.UserId,
                    CreatedAt = _dateTime.Now
                };

                await _dbContext.AddAsync(warehouse);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully created warehouse: {WarehouseName} (ID: {WarehouseId})",
                    warehouse.Name, warehouse.Id);

                return await GetWarehouseByIdAsync(warehouse.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating warehouse {WarehouseName}: {ErrorMessage}",
                    warehouseDto.Name, ex.Message);
                throw;
            }
        }

        public async Task<WarehouseDto> UpdateWarehouseAsync(Guid id, WarehouseUpdateDto warehouseDto)
        {
            _logger.LogInformation("Updating warehouse: {WarehouseId}", id);

            try
            {
                var warehouse = await _dbContext.Warehouses
                    .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse update failed - Warehouse not found: {WarehouseId}", id);
                    throw new KeyNotFoundException($"Warehouse with ID {id} not found.");
                }

                // Check if warehouse name already exists (excluding current warehouse)
                if (await WarehouseNameExistsAsync(warehouseDto.Name, id))
                {
                    _logger.LogWarning("Warehouse update failed - Name already exists: {WarehouseName}", warehouseDto.Name);
                    throw new InvalidOperationException($"Warehouse with name '{warehouseDto.Name}' already exists.");
                }

                // Check if warehouse code already exists (excluding current warehouse)
                if (await WarehouseCodeExistsAsync(warehouseDto.Code, id))
                {
                    _logger.LogWarning("Warehouse update failed - Code already exists: {WarehouseCode}", warehouseDto.Code);
                    throw new InvalidOperationException($"Warehouse with code '{warehouseDto.Code}' already exists.");
                }

                _mapper.Map(warehouseDto, warehouse);
                warehouse.ModifiedBy = _currentUserService.UserId;
                warehouse.ModifiedAt = _dateTime.Now;

                _dbContext.Update(warehouse);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated warehouse: {WarehouseName} (ID: {WarehouseId})",
                    warehouse.Name, id);

                return await GetWarehouseByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating warehouse {WarehouseId}: {ErrorMessage}", id, ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteWarehouseAsync(Guid id)
        {
            _logger.LogInformation("Deleting warehouse: {WarehouseId}", id);

            try
            {
                var warehouse = await _dbContext.Warehouses
                    .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse delete failed - Warehouse not found: {WarehouseId}", id);
                    throw new KeyNotFoundException($"Warehouse with ID {id} not found.");
                }

                // Check if warehouse has active clients
                var hasActiveClients = await _dbContext.Clients
                    .AnyAsync(c => c.WarehouseId == id && !c.IsDeleted);

                if (hasActiveClients)
                {
                    _logger.LogWarning("Warehouse delete failed - Has active clients: {WarehouseId}", id);
                    throw new InvalidOperationException("Cannot delete warehouse that has active clients.");
                }

                warehouse.IsDeleted = true;
                warehouse.ModifiedBy = _currentUserService.UserId;
                warehouse.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted warehouse: {WarehouseName} (ID: {WarehouseId})",
                    warehouse.Name, id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting warehouse {WarehouseId}: {ErrorMessage}", id, ex.Message);
                throw;
            }
        }

        public async Task<bool> ActivateWarehouseAsync(Guid id, bool isActive)
        {
            _logger.LogInformation("Setting warehouse active status: {WarehouseId} to {IsActive}", id, isActive);

            try
            {
                var warehouse = await _dbContext.Warehouses
                    .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse activation failed - Warehouse not found: {WarehouseId}", id);
                    throw new KeyNotFoundException($"Warehouse with ID {id} not found.");
                }

                warehouse.IsActive = isActive;
                warehouse.ModifiedBy = _currentUserService.UserId;
                warehouse.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully set warehouse {WarehouseName} (ID: {WarehouseId}) active status to {IsActive}",
                    warehouse.Name, id, isActive);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting warehouse {WarehouseId} active status: {ErrorMessage}", id, ex.Message);
                throw;
            }
        }

        public async Task<WarehouseConfigurationDto> GetWarehouseConfigurationAsync(Guid warehouseId)
        {
            _logger.LogDebug("Getting warehouse configuration: {WarehouseId}", warehouseId);

            try
            {
                var configuration = await _dbContext.WarehouseConfigurations
                    .FirstOrDefaultAsync(c => c.WarehouseId == warehouseId);

                if (configuration == null)
                {
                    _logger.LogWarning("Warehouse configuration not found: {WarehouseId}", warehouseId);
                    throw new KeyNotFoundException($"Configuration for warehouse with ID {warehouseId} not found.");
                }

                var configDto = _mapper.Map<WarehouseConfigurationDto>(configuration);

                _logger.LogInformation("Retrieved warehouse configuration for warehouse: {WarehouseId}", warehouseId);
                return configDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving warehouse configuration {WarehouseId}: {ErrorMessage}",
                    warehouseId, ex.Message);
                throw;
            }
        }

        public async Task<WarehouseConfigurationDto> UpdateWarehouseConfigurationAsync(Guid warehouseId,
            WarehouseConfigurationUpdateDto configDto)
        {
            _logger.LogInformation("Updating warehouse configuration: {WarehouseId}", warehouseId);

            try
            {
                var configuration = await _dbContext.WarehouseConfigurations
                    .FirstOrDefaultAsync(c => c.WarehouseId == warehouseId);

                if (configuration == null)
                {
                    _logger.LogWarning("Warehouse configuration update failed - Configuration not found: {WarehouseId}", warehouseId);
                    throw new KeyNotFoundException($"Configuration for warehouse with ID {warehouseId} not found.");
                }

                _mapper.Map(configDto, configuration);

                _dbContext.Update(configuration);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated warehouse configuration for warehouse: {WarehouseId}", warehouseId);

                return await GetWarehouseConfigurationAsync(warehouseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating warehouse configuration {WarehouseId}: {ErrorMessage}",
                    warehouseId, ex.Message);
                throw;
            }
        }

        public async Task<bool> WarehouseExistsAsync(Guid id)
        {
            return await _dbContext.Warehouses
                .AnyAsync(w => w.Id == id && !w.IsDeleted);
        }

        public async Task<bool> WarehouseNameExistsAsync(string name, Guid? excludeId = null)
        {
            return await _dbContext.Warehouses
                .AnyAsync(w => w.Name == name && !w.IsDeleted && (excludeId == null || w.Id != excludeId));
        }

        public async Task<bool> WarehouseCodeExistsAsync(string code, Guid? excludeId = null)
        {
            return await _dbContext.Warehouses
                .AnyAsync(w => w.Code == code && !w.IsDeleted && (excludeId == null || w.Id != excludeId));
        }

        #region Private Methods

        private IQueryable<Warehouse> ApplySorting(IQueryable<Warehouse> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                0 => sortDescending ? query.OrderByDescending(w => w.Name) : query.OrderBy(w => w.Name),
                1 => sortDescending ? query.OrderByDescending(w => w.Code) : query.OrderBy(w => w.Code),
                2 => sortDescending ? query.OrderByDescending(w => w.City) : query.OrderBy(w => w.City),
                3 => sortDescending ? query.OrderByDescending(w => w.State) : query.OrderBy(w => w.State),
                4 => sortDescending ? query.OrderByDescending(w => w.Country) : query.OrderBy(w => w.Country),
                5 => sortDescending ? query.OrderByDescending(w => w.ContactPerson) : query.OrderBy(w => w.ContactPerson),
                6 => sortDescending ? query.OrderByDescending(w => w.IsActive) : query.OrderBy(w => w.IsActive),
                7 => sortDescending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt),
                _ => sortDescending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt)
            };
        }

        #endregion
    }
}