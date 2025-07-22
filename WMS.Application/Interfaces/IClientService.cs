using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Clients;

namespace WMS.Application.Interfaces
{
    public interface IClientService
    {
        // Add this new method for pagination
        Task<PaginatedResult<ClientDto>> GetPaginatedClients(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending);

        // Existing methods
        Task<List<ClientDto>> GetAllClientsAsync();
        Task<List<ClientDto>> GetClientsByWarehouseIdAsync(Guid warehouseId);
        Task<ClientDto> GetClientByIdAsync(Guid id);
        Task<ClientDto> CreateClientAsync(ClientCreateDto clientDto);
        Task<ClientDto> UpdateClientAsync(Guid id, ClientUpdateDto clientDto);
        Task<bool> DeleteClientAsync(Guid id);
        Task<bool> ActivateClientAsync(Guid id, bool isActive);
        Task<bool> ClientExistsAsync(Guid id);
        Task<bool> ClientNameExistsAsync(string name, Guid? excludeId = null);
        Task<bool> ClientCodeExistsAsync(string code, Guid? excludeId = null);
    }
}