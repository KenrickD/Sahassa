using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Roles;
using WMS.Domain.DTOs.Services;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;
using WMS.WebApp.Models.Permissions;

namespace WMS.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            ILogger<RoleService> logger,
            IDateTime dateTime)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _logger = logger;
            _dateTime = dateTime;
        }
        public async Task<PaginatedResult<RoleDto>> GetPaginatedRoles(
         string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated Roles: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
       searchTerm, skip, take, sortColumn, sortAscending);

            // Get current warehouse ID from tenant service
            var warehouseId = _currentUserService.CurrentWarehouseId;
            _logger.LogDebug("Filtering Roles by warehouse: {WarehouseId}", warehouseId);

            try
            {
                // Start with warehouse-specific query
                var query = _dbContext.Roles
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(r => r.UserRoles)
                    .Where(u => !u.IsDeleted)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogDebug("Applying search filter: {SearchTerm}", searchTerm);

                    query = query.Where(u =>
                        u.Name.ToLower().Contains(searchTerm) ||
                        u.Description.ToLower().Contains(searchTerm)
                    );
                }

                // Apply sorting
                query = ApplySorting(query, sortColumn, !sortAscending);

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total Roles matching criteria: {TotalCount}", totalCount);

                // Apply pagination
                var Roles = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
                var RoleDtos = _mapper.Map<List<RoleDto>>(Roles.ToList());

                _logger.LogInformation("Retrieved {RoleCount} paginated Roles (skip={Skip}, take={Take}) from total of {TotalCount}",
                    RoleDtos.Count, skip, take, totalCount);

                return new PaginatedResult<RoleDto>
                {
                    Items = RoleDtos,
                    TotalCount = totalCount,
                    FilteredCount = totalCount // If searching, this would be different
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated Roles: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _dbContext.Set<Role>()
                .Where(r => !r.IsDeleted)
                .Select(r => new Role
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy,
                    ModifiedAt = r.ModifiedAt,
                    ModifiedBy = r.ModifiedBy,
                    UserRoles = r.UserRoles.Where(ur => !ur.User.IsDeleted).ToList(),
                    RolePermissions = r.RolePermissions.ToList()
                })
                .ToListAsync();

            return _mapper.Map<List<RoleDto>>(roles);
        }

        public async Task<RoleDto> GetRoleByIdAsync(Guid id)
        {
            var role = await _dbContext.Set<Role>()
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (role == null)
                throw new KeyNotFoundException($"Role with ID {id} not found.");

            var roleDto = _mapper.Map<RoleDto>(role);

            // Map permissions
            roleDto.Permissions = role.RolePermissions?
                .Select(rp => _mapper.Map<PermissionDto>(rp.Permission))
                .ToList();

            // Count active users in this role
            roleDto.UserCount = role.UserRoles == null ? 0 : role.UserRoles!.Count;
            roleDto.PermissionCount = roleDto.Permissions == null ? 0 : roleDto.Permissions!.Count;

            return roleDto;
        }

        public async Task<RoleDto> CreateRoleAsync(RoleCreateDto roleDto)
        {
            var role = _mapper.Map<Role>(roleDto);
            role.Id = Guid.NewGuid();

            await _dbContext.AddAsync(role);
            await _dbContext.SaveChangesAsync();

            // Add permissions if provided
            if (roleDto.PermissionIds != null && roleDto.PermissionIds.Any())
            {
                foreach (var permissionId in roleDto.PermissionIds)
                {
                    await _dbContext.Set<RolePermission>().AddAsync(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = permissionId
                    });
                }
                await _dbContext.SaveChangesAsync();
            }

            return await GetRoleByIdAsync(role.Id);
        }

        public async Task<RoleDto> UpdateRoleAsync(Guid id, RoleUpdateDto roleDto)
        {
            var role = await _dbContext.Roles.FindAsync(id);

            if (role == null)
                throw new KeyNotFoundException($"Role with ID {id} not found.");

            // Don't allow updating system roles
            if (role.IsSystemRole)
                throw new InvalidOperationException("System roles cannot be updated.");

            _mapper.Map(roleDto, role);

            _dbContext.Update(role);
            await _dbContext.SaveChangesAsync();

            // Update permissions if provided
            if (roleDto.PermissionIds != null)
            {
                await UpdateRolePermissionsAsync(id, roleDto.PermissionIds);
            }

            return await GetRoleByIdAsync(id);
        }

        public async Task<bool> DeleteRoleAsync(Guid id)
        {
            var role = await _dbContext.Roles.FindAsync(id);
            var userRoles = await _dbContext.UserRoles.Where(x => x.RoleId == id).ToListAsync();

            if (role == null)
                throw new KeyNotFoundException($"Role with ID {id} not found.");

            // Don't allow deleting system roles
            if (role.IsSystemRole)
                throw new InvalidOperationException("System roles cannot be deleted.");

            if (userRoles != null && userRoles?.Count > 0)
                throw new InvalidOperationException($"Role '{role.Name}' is still in use by users.");

            role.IsDeleted = true;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _dbContext.Set<Permission>()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return _mapper.Map<List<PermissionDto>>(permissions);
        }

        public async Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
        {
            // Get all permissions
            var allPermissions = await _dbContext.Set<Permission>()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                .ToListAsync();

            // Get role permissions
            var rolePermissionIds = await _dbContext.Set<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            // Map and mark assigned permissions
            var permissionDtos = _mapper.Map<List<PermissionDto>>(allPermissions);
            foreach (var permission in permissionDtos)
            {
                permission.IsAssigned = rolePermissionIds.Contains(permission.Id);
            }

            return permissionDtos;
        }

        public async Task<bool> UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds)
        {
            // Check if role exists
            if (!await _dbContext.Set<Role>().AnyAsync(r => r.Id == roleId && !r.IsDeleted))
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");

            // Get existing role permissions
            var existingPermissions = await _dbContext.Set<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            // Permissions to remove
            var permissionsToRemove = existingPermissions
                .Where(rp => !permissionIds.Contains(rp.PermissionId))
                .ToList();

            if (permissionsToRemove.Any())
            {
                _dbContext.RemoveRange(permissionsToRemove);
            }

            // Permissions to add
            var existingPermissionIds = existingPermissions.Select(rp => rp.PermissionId).ToList();
            var permissionsToAdd = permissionIds
                .Where(id => !existingPermissionIds.Contains(id))
                .Select(id => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = id,
                    CreatedBy = _currentUserService.UserId,
                    CreatedAt = _dateTime.Now
                })
                .ToList();

            if (permissionsToAdd.Any())
            {
                await _dbContext.AddRangeAsync(permissionsToAdd);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RoleExistsAsync(Guid id)
        {
            return await _dbContext.Set<Role>()
                .AnyAsync(r => r.Id == id && !r.IsDeleted);
        }

        public async Task<bool> RoleNameExistsAsync(string name, Guid? excludeId = null)
        {
            return await _dbContext.Set<Role>()
                .AnyAsync(r => r.Name == name && !r.IsDeleted && (excludeId == null || r.Id != excludeId));
        }

        public async Task<List<RoleDto>> GetRolesByUserIdAsync(Guid userId)
        {
            var roles = await _dbContext.Set<UserRole>()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .Where(r => !r.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<RoleDto>>(roles);
        }
        public async Task<RolePermissionsDataResultDto> GetRolePermissionsDataAsync(Guid roleId)
        {
            try
            {
                // Get all permissions
                var allPermissions = await _dbContext.Permissions
                    .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                    .ToListAsync();

                // Get Role's current permissions
                var RolePermissionIds = await _dbContext.RolePermissions
                    .Where(up => up.RoleId == roleId)
                    .Select(up => up.PermissionId)
                    .ToListAsync();

                return new RolePermissionsDataResultDto
                {
                    AllPermissions = allPermissions,
                    RolePermissionIds = RolePermissionIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Role permissions data for Role {RoleId}", roleId);
                throw;
            }
        }

        public async Task<ServiceResultDto> SaveRolePermissionChangesAsync(
        Guid roleId,
        List<PermissionChangeRequestDto> changes,
            string currentRoleId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Check if Role exists
                var Role = await _dbContext.Roles.FindAsync(roleId);
                if (Role == null)
                {
                    return ServiceResultDto.Failure("Role not found");
                }

                var addedCount = 0;
                var removedCount = 0;
                var errors = new List<string>();

                foreach (var change in changes)
                {
                    if (change.Action.ToLower() == "add")
                    {
                        var addResult = await AddRolePermissionInternalAsync(roleId, change.PermissionId, currentRoleId);
                        if (addResult.IsSuccess)
                        {
                            addedCount++;
                        }
                        else
                        {
                            errors.Add($"Failed to add permission: {addResult.ErrorMessage}");
                        }
                    }
                    else if (change.Action.ToLower() == "remove")
                    {
                        var removeResult = await RemoveRolePermissionInternalAsync(roleId, change.PermissionId, currentRoleId);
                        if (removeResult.IsSuccess)
                        {
                            removedCount++;
                        }
                        else
                        {
                            errors.Add($"Failed to remove permission: {removeResult.ErrorMessage}");
                        }
                    }
                }

                // Only commit if there were no errors
                if (errors.Any())
                {
                    await transaction.RollbackAsync();
                    return ServiceResultDto.Failure($"Some changes failed: {string.Join(", ", errors)}");
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var summary = new List<string>();
                if (addedCount > 0) summary.Add($"{addedCount} permission(s) added");
                if (removedCount > 0) summary.Add($"{removedCount} permission(s) removed");

                var successMessage = summary.Any() ? string.Join(", ", summary) : "No changes made";

                _logger.LogInformation("Role permissions updated for Role {RoleId}: {Summary} by {CurrentRoleId}",
                    roleId, successMessage, currentRoleId);

                return ServiceResultDto.Success(successMessage, successMessage);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving Role permission changes for Role {RoleId}", roleId);
                return ServiceResultDto.Failure("Failed to save permission changes. Please try again.");
            }
        }

        private async Task<ServiceResultDto> AddRolePermissionInternalAsync(Guid roleId, Guid permissionId, string currentRoleId)
        {
            try
            {
                // Check if permission exists
                var permission = await _dbContext.Permissions.FindAsync(permissionId);
                if (permission == null)
                {
                    return ServiceResultDto.Failure("Permission not found");
                }

                // Check if Role already has this permission
                var existingRolePermission = await _dbContext.RolePermissions
                    .FirstOrDefaultAsync(up => up.RoleId == roleId && up.PermissionId == permissionId);

                if (existingRolePermission != null)
                {
                    return ServiceResultDto.Success("Permission already exists"); // Not an error, just skip
                }

                // Add the permission
                var RolePermission = new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = permissionId
                };

                _dbContext.RolePermissions.Add(RolePermission);
                return ServiceResultDto.Success($"Added {permission.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding permission {PermissionId} to Role {RoleId}", permissionId, roleId);
                return ServiceResultDto.Failure($"Failed to add permission");
            }
        }

        private async Task<ServiceResultDto> RemoveRolePermissionInternalAsync(Guid roleId, Guid permissionId, string currentRoleId)
        {
            try
            {
                // Find the Role permission
                var RolePermission = await _dbContext.RolePermissions
                    .Include(up => up.Permission)
                    .FirstOrDefaultAsync(up => up.RoleId == roleId && up.PermissionId == permissionId);

                if (RolePermission == null)
                {
                    return ServiceResultDto.Success("Permission already removed"); // Not an error, just skip
                }

                // Remove the permission
                _dbContext.RolePermissions.Remove(RolePermission);
                return ServiceResultDto.Success($"Removed {RolePermission.Permission.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission {PermissionId} from Role {RoleId}", permissionId, roleId);
                return ServiceResultDto.Failure($"Failed to remove permission");
            }
        }

        private IQueryable<Role> ApplySorting(IQueryable<Role> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                2 => sortDescending ? query.OrderByDescending(c => c.Description) : query.OrderBy(c => c.Description),
                _ => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
            };
        }
    }
}