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
using WMS.Domain.DTOs.Clients;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;

namespace WMS.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly ILogger<ClientService> _logger;

        public ClientService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IDateTime dateTime,
            ILogger<ClientService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dateTime = dateTime;
            _logger = logger;
        }

        public async Task<PaginatedResult<ClientDto>> GetPaginatedClients(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated clients: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                searchTerm, skip, take, sortColumn, sortAscending);

            try
            {
                // Start with base query using tenant filter
                var query = _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .Include(c => c.Warehouse)
                    .AsQueryable();

                // Apply search if provided 
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogDebug("Applying search filter: {SearchTerm}", searchTerm);

                    query = query.Where(c =>
                        c.Name.ToLower().Contains(searchTerm) ||
                        (c.Warehouse != null && c.Warehouse.Name.ToLower().Contains(searchTerm)) ||
                        (c.Code != null && c.Code.ToLower().Contains(searchTerm)) ||
                        (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(searchTerm)) ||
                        (c.ContactEmail != null && c.ContactEmail.ToLower().Contains(searchTerm))
                    );
                }

                // Apply sorting
                query = ApplySorting(query, sortColumn, !sortAscending);

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total clients matching criteria: {TotalCount}", totalCount);

                // Apply pagination
                var clients = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var clientDtos = _mapper.Map<List<ClientDto>>(clients);

                _logger.LogInformation("Retrieved {ClientCount} paginated clients (skip={Skip}, take={Take}) from total of {TotalCount}",
                    clientDtos.Count, skip, take, totalCount);

                return new PaginatedResult<ClientDto>
                {
                    Items = clientDtos,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated clients: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private IQueryable<Client> ApplySorting(IQueryable<Client> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(c => c.Warehouse.Name) : query.OrderBy(c => c.Warehouse.Name),
                2 => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                3 => sortDescending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
                4 => sortDescending ? query.OrderByDescending(c => c.ContactPerson) : query.OrderBy(c => c.ContactPerson),
                5 => sortDescending ? query.OrderByDescending(c => c.ContactEmail) : query.OrderBy(c => c.ContactEmail),
                6 => sortDescending ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
                7 => sortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                _ => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
            };
        }

        public async Task<List<ClientDto>> GetAllClientsAsync()
        {
            _logger.LogDebug("Getting all clients");

            try
            {
                var clients = await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .ApplyClientFilter(_currentUserService)
                    .Include(c => c.Warehouse)
                    .ToListAsync();

                var result = _mapper.Map<List<ClientDto>>(clients);

                _logger.LogInformation("Retrieved {ClientCount} clients", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all clients");
                throw;
            }
        }

        public async Task<List<ClientDto>> GetClientsByWarehouseIdAsync(Guid warehouseId)
        {
            _logger.LogDebug("Getting clients for warehouse: {WarehouseId}", warehouseId);

            try
            {
                var clients = await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .Include(c => c.Warehouse)
                    .Where(c => c.WarehouseId == warehouseId)
                    .ToListAsync();

                var result = _mapper.Map<List<ClientDto>>(clients);

                _logger.LogInformation("Retrieved {ClientCount} clients for warehouse {WarehouseId}",
                    result.Count, warehouseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clients for warehouse {WarehouseId}", warehouseId);
                throw;
            }
        }

        public async Task<ClientDto> GetClientByIdAsync(Guid id)
        {
            _logger.LogDebug("Getting client by ID: {ClientId}", id);

            try
            {
                var client = await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .Include(c => c.Warehouse)
                    .Include(c => c.Configuration)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null)
                {
                    _logger.LogWarning("Client not found: {ClientId}", id);
                    throw new KeyNotFoundException($"Client with ID {id} not found.");
                }

                var result = _mapper.Map<ClientDto>(client);
                _logger.LogDebug("Successfully retrieved client: {ClientId} - {ClientName}", id, result.Name);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client {ClientId}", id);
                throw;
            }
        }

        public async Task<ClientDto> CreateClientAsync(ClientCreateDto clientDto)
        {
            _logger.LogInformation("Creating new client: {ClientName}", clientDto.Name);

            // Validate unique constraints
            if (await ClientNameExistsAsync(clientDto.Name))
            {
                var message = $"Client name '{clientDto.Name}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (!string.IsNullOrEmpty(clientDto.Code) && await ClientCodeExistsAsync(clientDto.Code))
            {
                var message = $"Client code '{clientDto.Code}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var client = _mapper.Map<Client>(clientDto);
                client.Id = Guid.NewGuid();
                client.CreatedBy = _currentUserService.UserId;
                client.CreatedAt = _dateTime.Now;

                // Set up client configuration
                client.Configuration = new ClientConfiguration
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    RequiresQualityCheck = clientDto.RequiresQualityCheck,
                    AutoGenerateReceivingLabels = clientDto.AutoGenerateReceivingLabels,
                    AutoGenerateShippingLabels = clientDto.AutoGenerateShippingLabels,
                    HandlingFeePercentage = clientDto.HandlingFeePercentage,
                    StorageFeePerCubicMeter = clientDto.StorageFeePerCubicMeter,
                    DefaultLeadTimeDays = clientDto.DefaultLeadTimeDays,
                    LowStockThreshold = clientDto.LowStockThreshold,
                    SendLowStockAlerts = clientDto.SendLowStockAlerts,
                    AllowPartialShipments = clientDto.AllowPartialShipments,
                    RequiresAppointmentForReceiving = clientDto.RequiresAppointmentForReceiving,
                    CreatedBy = _currentUserService.UserId,
                    CreatedAt = _dateTime.Now
                };

                await _dbContext.AddAsync(client);
                await _dbContext.SaveChangesAsync();

                // Reload client with warehouse information
                await _dbContext.Entry(client).Reference(c => c.Warehouse).LoadAsync();

                await transaction.CommitAsync();

                var result = _mapper.Map<ClientDto>(client);
                _logger.LogInformation("Client created successfully: {ClientId} - {ClientName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating client: {ClientName}", clientDto.Name);
                throw;
            }
        }

        public async Task<ClientDto> UpdateClientAsync(Guid id, ClientUpdateDto clientDto)
        {
            _logger.LogInformation("Updating client: {ClientId} - {ClientName}", id, clientDto.Name);

            // Validate unique constraints
            if (await ClientNameExistsAsync(clientDto.Name, id))
            {
                var message = $"Client name '{clientDto.Name}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            if (!string.IsNullOrEmpty(clientDto.Code) && await ClientCodeExistsAsync(clientDto.Code, id))
            {
                var message = $"Client code '{clientDto.Code}' already exists";
                _logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var client = await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .Include(c => c.Warehouse)
                    .Include(c => c.Configuration)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null)
                {
                    var message = $"Client with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Update client properties
                _mapper.Map(clientDto, client);
                client.ModifiedBy = _currentUserService.UserId;
                client.ModifiedAt = _dateTime.Now;

                // Update configuration
                if (client.Configuration != null)
                {
                    client.Configuration.RequiresQualityCheck = clientDto.RequiresQualityCheck;
                    client.Configuration.AutoGenerateReceivingLabels = clientDto.AutoGenerateReceivingLabels;
                    client.Configuration.AutoGenerateShippingLabels = clientDto.AutoGenerateShippingLabels;
                    client.Configuration.HandlingFeePercentage = clientDto.HandlingFeePercentage;
                    client.Configuration.StorageFeePerCubicMeter = clientDto.StorageFeePerCubicMeter;
                    client.Configuration.DefaultLeadTimeDays = clientDto.DefaultLeadTimeDays;
                    client.Configuration.LowStockThreshold = clientDto.LowStockThreshold;
                    client.Configuration.SendLowStockAlerts = clientDto.SendLowStockAlerts;
                    client.Configuration.AllowPartialShipments = clientDto.AllowPartialShipments;
                    client.Configuration.RequiresAppointmentForReceiving = clientDto.RequiresAppointmentForReceiving;
                    client.Configuration.ModifiedBy = _currentUserService.UserId;
                    client.Configuration.ModifiedAt = _dateTime.Now;
                }

                _dbContext.Update(client);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = _mapper.Map<ClientDto>(client);
                _logger.LogInformation("Client updated successfully: {ClientId} - {ClientName}",
                    result.Id, result.Name);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating client: {ClientId} - {ClientName}", id, clientDto.Name);
                throw;
            }
        }

        public async Task<bool> DeleteClientAsync(Guid id)
        {
            _logger.LogInformation("Deleting client: {ClientId}", id);

            try
            {
                var client = await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null)
                {
                    var message = $"Client with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                // Check if client has associated data
                var hasUsers = await _dbContext.Users.AnyAsync(u => u.ClientId == id && !u.IsDeleted);
                var hasProducts = await _dbContext.Products.AnyAsync(p => p.ClientId == id && !p.IsDeleted);

                if (hasUsers || hasProducts)
                {
                    var message = $"Cannot delete client '{client.Name}' as it has associated users or products";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                client.IsDeleted = true;
                client.ModifiedBy = _currentUserService.UserId;
                client.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Client deleted successfully: {ClientId} - {ClientName}",
                    id, client.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client: {ClientId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateClientAsync(Guid id, bool isActive)
        {
            _logger.LogInformation("Changing client status: {ClientId} to {IsActive}", id, isActive);

            try
            {
                var client = await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null)
                {
                    var message = $"Client with ID {id} not found";
                    _logger.LogWarning(message);
                    throw new KeyNotFoundException(message);
                }

                client.IsActive = isActive;
                client.ModifiedBy = _currentUserService.UserId;
                client.ModifiedAt = _dateTime.Now;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Client status changed successfully: {ClientId} - {ClientName} to {IsActive}",
                    id, client.Name, isActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing client status: {ClientId} to {IsActive}", id, isActive);
                throw;
            }
        }

        public async Task<bool> ClientExistsAsync(Guid id)
        {
            try
            {
                return await _dbContext.Clients
                    .ApplyTenantFilter(_currentUserService)
                    .AnyAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if client exists: {ClientId}", id);
                throw;
            }
        }

        public async Task<bool> ClientNameExistsInWarehouseAsync(string name, Guid warehouseId, Guid? excludeId = null)
        {
            return await _dbContext.Set<Client>()
                .AnyAsync(c => c.Name == name &&
                              c.WarehouseId == warehouseId &&
                              !c.IsDeleted &&
                              (excludeId == null || c.Id != excludeId));
        }

        public async Task<bool> ClientCodeExistsInWarehouseAsync(string code, Guid warehouseId, Guid? excludeId = null)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            return await _dbContext.Set<Client>()
                .AnyAsync(c => c.Code == code &&
                              c.WarehouseId == warehouseId &&
                              !c.IsDeleted &&
                              (excludeId == null || c.Id != excludeId));
        }

        public async Task<List<ClientDto>> GetClientsByWarehouseIdAsync(Guid warehouseId, bool activeOnly = false)
        {
            var query = _dbContext.Set<Client>()
                .Include(c => c.Warehouse)
                .Where(c => c.WarehouseId == warehouseId && !c.IsDeleted);

            if (activeOnly)
            {
                query = query.Where(c => c.IsActive);
            }

            var clients = await query.ToListAsync();
            return _mapper.Map<List<ClientDto>>(clients);
        }

        public async Task<int> GetClientCountByWarehouseAsync(Guid warehouseId)
        {
            return await _dbContext.Set<Client>()
                .CountAsync(c => c.WarehouseId == warehouseId && !c.IsDeleted && c.IsActive);
        }

        // Update the existing ClientNameExistsAsync and ClientCodeExistsAsync methods to be warehouse-aware
        public async Task<bool> ClientNameExistsAsync(string name, Guid? excludeId = null)
        {
            // Get current user's warehouse context
            var warehouseId = _currentUserService.CurrentWarehouseId;

            return await _dbContext.Set<Client>()
                .AnyAsync(c => c.Name == name &&
                              c.WarehouseId == warehouseId &&
                              !c.IsDeleted &&
                              (excludeId == null || c.Id != excludeId));
        }

        public async Task<bool> ClientCodeExistsAsync(string code, Guid? excludeId = null)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            // Get current user's warehouse context
            var warehouseId = _currentUserService.CurrentWarehouseId;

            return await _dbContext.Set<Client>()
                .AnyAsync(c => c.Code == code &&
                              c.WarehouseId == warehouseId &&
                              !c.IsDeleted &&
                              (excludeId == null || c.Id != excludeId));
        }
    }
}