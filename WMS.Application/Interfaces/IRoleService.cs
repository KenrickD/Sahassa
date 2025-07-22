using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Roles;
using WMS.Domain.DTOs.Services;
using WMS.Domain.DTOs.Users;
using WMS.WebApp.Models.Permissions;

namespace WMS.Application.Interfaces
{
    public interface IRoleService
    {
        Task<PaginatedResult<RoleDto>> GetPaginatedRoles(string searchTerm, int skip, int take, int sortColumn, bool sortAscending);
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<RoleDto> GetRoleByIdAsync(Guid id);
        Task<RoleDto> CreateRoleAsync(RoleCreateDto roleDto);
        Task<RoleDto> UpdateRoleAsync(Guid id, RoleUpdateDto roleDto);
        Task<bool> DeleteRoleAsync(Guid id);
        Task<List<PermissionDto>> GetAllPermissionsAsync();
        Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId);
        Task<bool> UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds);
        Task<bool> RoleExistsAsync(Guid id);
        Task<bool> RoleNameExistsAsync(string name, Guid? excludeId = null);
        Task<List<RoleDto>> GetRolesByUserIdAsync(Guid userId);

        // Role Permission Management
        Task<RolePermissionsDataResultDto> GetRolePermissionsDataAsync(Guid roleId);
        Task<ServiceResultDto> SaveRolePermissionChangesAsync(
            Guid roleId,
            List<PermissionChangeRequestDto> changes,
            string currentRoleId);
    }
}