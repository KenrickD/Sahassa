using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WMS.Application.Extensions;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Services;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;
using WMS.WebApp.Models.Permissions;
using WMS.WebApp.Models.Users;

namespace WMS.Application.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UserService> _logger;
        private readonly IPasswordHasher<User> _passwordHasher;
        public UserService(
            AppDbContext context,
            IWebHostEnvironment environment,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IPasswordHasher<User> passwordHasher,
            ILogger<UserService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _passwordHasher = passwordHasher;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        private async Task<User?> GetByIdAsync(Guid id)
        {
            User? user = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Client)
                    .Include(u => u.Warehouse)
                    .Include(u => u.UserRoles).ThenInclude(u => u == null ? null : u.Role)
                    .Include(u => u.UserPermissions).ThenInclude(u => u == null ? null : u.Permission)
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return null;

            return user;
        }
        public async Task<PaginatedResult<UserDto>> GetPaginatedUsers(
          string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated users: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
       searchTerm, skip, take, sortColumn, sortAscending);

            // Get current warehouse ID from tenant service
            var warehouseId = _currentUserService.CurrentWarehouseId;
            _logger.LogDebug("Filtering users by warehouse: {WarehouseId}", warehouseId);

            try
            {
                // Start with warehouse-specific query
                var query = _context.Users
                    .ApplyTenantFilter(_currentUserService)
                    .Include(u => u.Client)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    _logger.LogDebug("Applying search filter: {SearchTerm}", searchTerm);

                    query = query.Where(u =>
                        u.Username.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.FirstName.ToLower().Contains(searchTerm) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                        (u.Client != null && u.Client.Name.ToLower().Contains(searchTerm))
                    );
                }

                // Apply sorting
                query = ApplySorting(query, sortColumn, !sortAscending);

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total users matching criteria: {TotalCount}", totalCount);

                // Apply pagination
                var users = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
                var userDtos = _mapper.Map<List<UserDto>>(users.ToList());

                _logger.LogInformation("Retrieved {UserCount} paginated users (skip={Skip}, take={Take}) from total of {TotalCount}",
                    userDtos.Count, skip, take, totalCount);

                return new PaginatedResult<UserDto>
                {
                    Items = userDtos,
                    TotalCount = totalCount,
                    FilteredCount = totalCount // If searching, this would be different
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated users: {ErrorMessage}", ex.Message);
                throw;
            }
        }
        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await GetByIdAsync(id);

            if (user == null)
                return null;

            var userDTOMapped = MapUserToDto(user);

            var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();
            var permissions = new List<string>();

            // Get direct permissions
            if (user.UserPermissions != null)
            {
                permissions.AddRange(user.UserPermissions.Select(up => up.Permission.Name));
            }

            // Get role permissions
            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    var rolePermissions = await _context.RolePermissions
                        .Include(rp => rp.Permission)
                        .Where(rp => rp.RoleId == userRole.RoleId)
                        .Select(rp => rp.Permission.Name)
                        .ToListAsync();

                    permissions.AddRange(rolePermissions);
                }
            }

            permissions = permissions.Distinct().ToList();

            userDTOMapped.Permissions.AddRange(permissions);

            return userDTOMapped;
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Client)
                .Include(u => u.Warehouse)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

            if (user == null)
                return null;

            return MapUserToDto(user);
        }

        public async Task<UserDto> CreateUserAsync(UserCreateDto userDto)
        {
            if (userDto == null)
            {
                _logger.LogWarning("Create user attempted with null UserCreateDto");
                throw new ArgumentNullException(nameof(userDto));
            }
            IFormFile? profileImage = userDto.ProfileImage;

            // Log the creation attempt with relevant information (excluding password)
            _logger.LogInformation(
                "User creation requested: Username={Username}, Email={Email}, FirstName={FirstName}, ClientId={ClientId}, WarehouseId={WarehouseId}, " +
                "HasProfileImage={HasProfileImage}, RequestedBy={RequestedBy}",
                userDto.Username,
                userDto.Email,
                userDto.FirstName,
                userDto.ClientId,
                userDto.WarehouseId,
                profileImage != null,
                _currentUserService.GetCurrentUsername ?? "System"
            );

            // Validate username uniqueness
            if (!await IsUsernameAvailableAsync(userDto.Username))
            {
                _logger.LogWarning("User creation failed: Username '{Username}' is already in use", userDto.Username);
                throw new InvalidOperationException($"Username '{userDto.Username}' is already in use.");
            }

            // Validate email uniqueness
            if (!await IsEmailAvailableAsync(userDto.Email))
            {
                _logger.LogWarning("User creation failed: Email '{Email}' is already in use", userDto.Email);
                throw new InvalidOperationException($"Email '{userDto.Email}' is already in use.");
            }

            // Start transaction for atomic operation
            using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogDebug("Transaction started for creating user {Username}", userDto.Username);

            try
            {
                // Generate ID early for logging purposes
                var userId = Guid.NewGuid();
                _logger.LogDebug("Generated new user ID: {UserId}", userId);

                // Map DTO to entity
                var user = new User
                {
                    Id = userId,
                    Username = userDto.Username,
                    Email = userDto.Email,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    PhoneNumber = userDto.PhoneNumber,
                    EmailConfirmed = true, //for now no need confirmation, just direct send generated password through email
                    ClientId = userDto.ClientId,
                    WarehouseId = userDto.WarehouseId,
                    IsActive = userDto.IsActive,
                    IsDeleted = false
                };

                // Upload profile image if provided
                if (profileImage != null)
                {
                    _logger.LogDebug("Uploading profile image for user {Username} (ID: {UserId})",
                        user.Username, user.Id);

                    try
                    {
                        user.ProfileImagePath = await UploadProfileImageAsync(profileImage);
                        _logger.LogDebug("Profile image uploaded successfully: {ProfileImagePath}", user.ProfileImagePath);
                    }
                    catch (Exception imageEx)
                    {
                        _logger.LogWarning(imageEx,
                            "Failed to upload profile image for user {Username} (ID: {UserId}): {ErrorMessage}",
                            user.Username, user.Id, imageEx.Message);
                        // Continue without profile image
                    }
                }

                // Generate new random password
                _logger.LogDebug("Generating random password for user {Username} (ID: {UserId})",
                    user.Username, user.Id);
                var newPassword = GenerateRandomPassword();

                // Hash and update password (never log the actual password)
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
                _logger.LogDebug("Password hashed and security stamp generated for user {Username} (ID: {UserId})",
                    user.Username, user.Id);

                // Add user to database
                await _context.Users.AddAsync(user);
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogDebug("User {Username} (ID: {UserId}) saved to database, {RowCount} rows affected",
                    user.Username, user.Id, saveResult);

                // Add user roles
                if (userDto.RoleIds.Any())
                {
                    _logger.LogDebug("Assigning {RoleCount} roles to user {Username} (ID: {UserId})",
                        userDto.RoleIds.Count, user.Username, user.Id);
                    await AddUserRolesAsync(user.Id, userDto.RoleIds);
                }
                else
                {
                    _logger.LogWarning("User {Username} (ID: {UserId}) created without any roles",
                        user.Username, user.Id);
                }

                // Commit transaction
                await transaction.CommitAsync();
                _logger.LogDebug("Transaction committed for user {Username} (ID: {UserId})",
                    user.Username, user.Id);

                // Log successful creation with user details
                _logger.LogInformation(
                    "User created successfully: ID={UserId}, Username={Username}, Email={Email}, " +
                    "WarehouseId={WarehouseId}, ClientId={ClientId}, HasRoles={HasRoles}, CreatedBy={CreatedBy}",
                    user.Id,
                    user.Username,
                    user.Email,
                    user.WarehouseId,
                    user.ClientId,
                    userDto.RoleIds.Any(),
                    user.CreatedBy
                );

                // Send password email
                try
                {
                    _logger.LogDebug("Sending new password email to {Email} for user {Username} (ID: {UserId})",
                        user.Email, user.Username, user.Id);
                    //await SendNewPasswordEmailAsync("arif@hsc.sg", newPassword);
                    await SendNewPasswordEmailAsync(user.Email, newPassword);
                    _logger.LogDebug("Password email sent successfully");
                }
                catch (Exception emailEx)
                {
                    // Log but don't fail the user creation if email fails
                    _logger.LogError(emailEx,
                        "Failed to send password email for new user {Username} (ID: {UserId}): {ErrorMessage}",
                        user.Username, user.Id, emailEx.Message);
                }

                // Reload user with roles to return complete DTO
                _logger.LogDebug("Reloading user with roles for user {Username} (ID: {UserId})",
                    user.Username, user.Id);

                var createdUser = await GetByIdAsync(user.Id);

                if (createdUser == null)
                {
                    _logger.LogWarning("User created but could not be reloaded: {Username} (ID: {UserId})",
                        user.Username, user.Id);
                    // Fallback to original user object
                    return MapUserToDto(user);
                }

                return MapUserToDto(createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating user {Username}: {ErrorType}: {ErrorMessage}",
                    userDto.Username, ex.GetType().Name, ex.Message);

                try
                {
                    await transaction.RollbackAsync();
                    _logger.LogDebug("Transaction rolled back for user creation: {Username}", userDto.Username);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx,
                        "Failed to rollback transaction for user creation {Username}: {ErrorMessage}",
                        userDto.Username, rollbackEx.Message);
                }

                throw; // Rethrow the original exception
            }
        }

        public async Task<UserDto> UpdateUserAsync(UserUpdateDto userDto)
        {
            if (userDto == null)
                throw new ArgumentNullException(nameof(userDto));

            IFormFile? profileImage = userDto.ProfileImage;

            // Get existing user
            var user = await _context.Users.FindAsync(userDto.Id);

            if (user == null)
                throw new KeyNotFoundException($"User with ID {userDto.Id} not found.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Update user properties
                user.FirstName = userDto.FirstName;
                user.LastName = userDto.LastName;
                user.PhoneNumber = userDto.PhoneNumber;

                if (userDto.HasEditAccess && !userDto.IsOwnProfile)
                {
                    user.ClientId = userDto.ClientId;
                    user.WarehouseId = userDto.WarehouseId;
                    user.IsActive = userDto.IsActive;
                }

                // Handle profile image
                if (profileImage != null)
                {
                    user.ProfileImagePath = await UploadProfileImageAsync(profileImage, user.ProfileImagePath);
                }
                else if (userDto.RemoveProfileImage)
                {
                    // Remove profile image if requested
                    if (!string.IsNullOrEmpty(user.ProfileImagePath))
                    {
                        DeleteProfileImage(user.ProfileImagePath);
                        user.ProfileImagePath = null;
                    }
                }

                // Update user
                //_context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Update user roles
                // Remove existing roles
                var existingRoles = await _context.UserRoles!
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync();

                _context.UserRoles!.RemoveRange(existingRoles);
                await _context.SaveChangesAsync();

                // Add new roles
                if (userDto.RoleIds.Any())
                {
                    await AddUserRolesAsync(user.Id, userDto.RoleIds);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("User {Username} updated successfully", user.Username);

                // Reload user with roles to return complete DTO
                var updatedUser = await GetByIdAsync(user.Id);

                return MapUserToDto(updatedUser!);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating user {Username}", userDto.Username);
                throw;
            }
        }
        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string confirmationPassword)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmationPassword))
            {
                throw new InvalidOperationException($"New password and confirmation are required.");
            }

            if (newPassword != confirmationPassword)
            {
                throw new InvalidOperationException($"New password and confirmation do not match.");
            }
            if (!string.IsNullOrEmpty(currentPassword))
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);

                if (verificationResult != PasswordVerificationResult.Success)
                {
                    throw new InvalidOperationException($"Current password is incorrect.");
                }
            }
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed for user {Username} ({Id})", user.Username, user.Id);
        }
        // Add these methods to your existing UserService class
        #region User roles
        public async Task<UserRolesDataResultDto> GetUserRolesDataAsync(Guid userId)
        {
            try
            {
                // Get all roles
                var allRoles = await _context.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                // Get user's current roles
                var userRoleIds = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                return new UserRolesDataResultDto
                {
                    AllRoles = allRoles,
                    UserRoleIds = userRoleIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles data for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ServiceResultDto> SaveUserRoleChangesAsync(
            Guid userId,
            List<RoleChangeRequestDto> changes,
            string currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ServiceResultDto.Failure("User not found");
                }

                var addedCount = 0;
                var removedCount = 0;
                var errors = new List<string>();

                foreach (var change in changes)
                {
                    if (change.Action.ToLower() == "add")
                    {
                        var addResult = await AddUserRoleInternalAsync(userId, change.RoleId, currentUserId);
                        if (addResult.IsSuccess)
                        {
                            addedCount++;
                        }
                        else
                        {
                            errors.Add($"Failed to add role: {addResult.ErrorMessage}");
                        }
                    }
                    else if (change.Action.ToLower() == "remove")
                    {
                        var removeResult = await RemoveUserRoleInternalAsync(userId, change.RoleId, currentUserId);
                        if (removeResult.IsSuccess)
                        {
                            removedCount++;
                        }
                        else
                        {
                            errors.Add($"Failed to remove role: {removeResult.ErrorMessage}");
                        }
                    }
                }

                // Only commit if there were no errors
                if (errors.Any())
                {
                    await transaction.RollbackAsync();
                    return ServiceResultDto.Failure($"Some changes failed: {string.Join(", ", errors)}");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var summary = new List<string>();
                if (addedCount > 0) summary.Add($"{addedCount} role(s) added");
                if (removedCount > 0) summary.Add($"{removedCount} role(s) removed");

                var successMessage = summary.Any() ? string.Join(", ", summary) : "No changes made";

                _logger.LogInformation("User roles updated for user {UserId}: {Summary} by {CurrentUserId}",
                    userId, successMessage, currentUserId);

                return ServiceResultDto.Success(successMessage, successMessage);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving user role changes for user {UserId}", userId);
                return ServiceResultDto.Failure("Failed to save role changes. Please try again.");
            }
        }

        private async Task<ServiceResultDto> AddUserRoleInternalAsync(Guid userId, Guid roleId, string currentUserId)
        {
            try
            {
                // Check if role exists
                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return ServiceResultDto.Failure("Role not found");
                }

                // Check if user already has this role
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (existingUserRole != null)
                {
                    return ServiceResultDto.Success("Role already exists"); // Not an error, just skip
                }

                // Add the role
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                _context.UserRoles.Add(userRole);
                return ServiceResultDto.Success($"Added {role.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding role {RoleId} to user {UserId}", roleId, userId);
                return ServiceResultDto.Failure($"Failed to add role");
            }
        }

        private async Task<ServiceResultDto> RemoveUserRoleInternalAsync(Guid userId, Guid roleId, string currentUserId)
        {
            try
            {
                // Find the user role
                var userRole = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (userRole == null)
                {
                    return ServiceResultDto.Success("Role already removed"); // Not an error, just skip
                }

                // Remove the role
                _context.UserRoles.Remove(userRole);
                return ServiceResultDto.Success($"Removed {userRole.Role.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return ServiceResultDto.Failure($"Failed to remove role");
            }
        }
        #endregion

        #region MyRegion
        public async Task<UserPermissionsDataResultDto> GetUserPermissionsDataAsync(Guid userId)
        {
            try
            {
                // Get all permissions
                var allPermissions = await _context.Permissions
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                // Get user's current permissions
                var userPermissionIds = await _context.UserPermissions
                    .Where(up => up.UserId == userId)
                    .Select(up => up.PermissionId)
                    .ToListAsync();

                return new UserPermissionsDataResultDto
                {
                    AllPermissions = allPermissions,
                    UserPermissionIds = userPermissionIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions data for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ServiceResultDto> SaveUserPermissionChangesAsync(
            Guid userId,
            List<PermissionChangeRequestDto> changes,
            string currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ServiceResultDto.Failure("User not found");
                }

                var addedCount = 0;
                var removedCount = 0;
                var errors = new List<string>();

                foreach (var change in changes)
                {
                    if (change.Action.ToLower() == "add")
                    {
                        var addResult = await AddUserPermissionInternalAsync(userId, change.PermissionId, currentUserId);
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
                        var removeResult = await RemoveUserPermissionInternalAsync(userId, change.PermissionId, currentUserId);
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

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var summary = new List<string>();
                if (addedCount > 0) summary.Add($"{addedCount} permission(s) added");
                if (removedCount > 0) summary.Add($"{removedCount} permission(s) removed");

                var successMessage = summary.Any() ? string.Join(", ", summary) : "No changes made";

                _logger.LogInformation("User permissions updated for user {UserId}: {Summary} by {CurrentUserId}",
                    userId, successMessage, currentUserId);

                return ServiceResultDto.Success(successMessage, successMessage);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving user permission changes for user {UserId}", userId);
                return ServiceResultDto.Failure("Failed to save permission changes. Please try again.");
            }
        }

        private async Task<ServiceResultDto> AddUserPermissionInternalAsync(Guid userId, Guid permissionId, string currentUserId)
        {
            try
            {
                // Check if permission exists
                var permission = await _context.Permissions.FindAsync(permissionId);
                if (permission == null)
                {
                    return ServiceResultDto.Failure("Permission not found");
                }

                // Check if user already has this permission
                var existingUserPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

                if (existingUserPermission != null)
                {
                    return ServiceResultDto.Success("Permission already exists"); // Not an error, just skip
                }

                // Add the permission
                var userPermission = new UserPermission
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PermissionId = permissionId
                };

                _context.UserPermissions.Add(userPermission);
                return ServiceResultDto.Success($"Added {permission.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding permission {PermissionId} to user {UserId}", permissionId, userId);
                return ServiceResultDto.Failure($"Failed to add permission");
            }
        }

        private async Task<ServiceResultDto> RemoveUserPermissionInternalAsync(Guid userId, Guid permissionId, string currentUserId)
        {
            try
            {
                // Find the user permission
                var userPermission = await _context.UserPermissions
                    .Include(up => up.Permission)
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

                if (userPermission == null)
                {
                    return ServiceResultDto.Success("Permission already removed"); // Not an error, just skip
                }

                // Remove the permission
                _context.UserPermissions.Remove(userPermission);
                return ServiceResultDto.Success($"Removed {userPermission.Permission.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission {PermissionId} from user {UserId}", permissionId, userId);
                return ServiceResultDto.Failure($"Failed to remove permission");
            }
        }
        #endregion

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new KeyNotFoundException($"User with ID {id} not found.");

            // Soft delete
            user.IsDeleted = true;
            user.IsActive = false; // Ensure the user can't log in anymore

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} ({Id}) deleted by {CurrentUser}",
                user.Username, user.Id, user.ModifiedBy);
        }

        public async Task<List<Role>> GetUserRolesAsync(Guid userId)
        {
            var roles = await _context.UserRoles!
                .AsNoTracking()
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .ToListAsync();

            return roles;
        }

        public async Task<string> UploadProfileImageAsync(IFormFile file, string? existingImagePath = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file", nameof(file));

            // Validate file type
            if (!IsValidImageFile(file))
                throw new ArgumentException("Invalid image file type. Only JPG, PNG, and GIF are allowed.");

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds the 5MB limit.");

            // Delete existing image if it exists
            if (!string.IsNullOrEmpty(existingImagePath))
            {
                DeleteProfileImage(existingImagePath);
            }

            // Create upload directory if it doesn't exist
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadDir);

            // Generate unique filename with original extension
            var fileName = $"{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path
            return $"/uploads/profiles/{fileName}";
        }

        public string GetProfileImageUrl(string? profileImagePath)
        {
            if (string.IsNullOrEmpty(profileImagePath))
            {
                return "/images/default-profile.png"; // Default profile image
            }

            // If path is already a URL, return it
            if (profileImagePath.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                return profileImagePath;
            }

            // Otherwise, return relative path
            return profileImagePath;
        }

        public async Task<string> ResetPasswordAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            // Generate random password
            var newPassword = GenerateRandomPassword();

            // In a real system, you'd hash the password and update it in your auth system

            // Update modification tracking
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = _currentUserService.GetCurrentUsername ?? "System";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset for user {Username} ({Id})", user.Username, user.Id);

            return newPassword;
        }
        private async Task SendNewPasswordEmailAsync(string email, string newPassword)
        {
            var subject = "Your New Password - Warehouse Management System";
            var body = $@"
                    <h2>New Password</h2>
                    <p>Your new password is: <strong>{newPassword}</strong></p>
                    <p>Please login with this new password and change it to something you can remember.</p>
                    <br/>
                ";

            List<string> recipients = new List<string>();
            recipients.Add(email);

            await EmailHelper.SendEmailAsync(recipients, subject, body);
        }
        public async Task<bool> IsUsernameAvailableAsync(string username, Guid? excludeUserId = null)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Username == username && !u.IsDeleted);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null)
        {
            //unique value, must check all records include deleted.
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync();
        }

        #region Private Helper Methods

        private UserDto MapUserToDto(User user)
        {
            var dto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfileImagePath = user.ProfileImagePath,
                ProfileImageUrl = GetProfileImageUrl(user.ProfileImagePath),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy,
                ModifiedAt = user.ModifiedAt,
                ModifiedBy = user.ModifiedBy,
                ClientId = user.ClientId,
                ClientName = user.Client?.Name,
                WarehouseId = user.WarehouseId,
                WarehouseName = user.Warehouse?.Name ?? string.Empty
            };

            // Map roles
            if (user.UserRoles != null)
            {
                dto.Roles = user.UserRoles
                    .Select(ur => ur.Role.Name)
                    .ToList();
                dto.CurrentRoleIds = user.UserRoles
                    .Select(ur => ur.Role.Id)
                    .ToList();
            }


            return dto;
        }

        private IQueryable<User> ApplyFilters(
            IQueryable<User> query,
            string? searchTerm,
            Guid? clientId,
            Guid? warehouseId,
            Guid? roleId,
            bool? isActive)
        {
            // Apply search term filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)));
            }

            // Apply client filter
            if (clientId.HasValue)
            {
                query = query.Where(u => u.ClientId == clientId);
            }

            // Apply warehouse filter
            if (warehouseId.HasValue)
            {
                query = query.Where(u => u.WarehouseId == warehouseId);
            }

            // Apply role filter
            if (roleId.HasValue)
            {
                query = query.Where(u => u.UserRoles!.Any(ur => ur.RoleId == roleId));
            }

            // Apply active status filter
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive);
            }

            return query;
        }

        private IQueryable<User> ApplySorting(IQueryable<User> query, int sortColumn, bool sortDescending)
        {
            return sortColumn switch
            {
                1 => sortDescending ? query.OrderByDescending(c => c.Username) : query.OrderBy(c => c.Username),
                2 => sortDescending ? query.OrderByDescending(c => c.FirstName) : query.OrderBy(c => c.FirstName),
                3 => sortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
                4 => sortDescending ? query.OrderByDescending(c => c.Client.Name) : query.OrderBy(c => c.Client.Name),
                5 => sortDescending ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
                _ => sortDescending ? query.OrderByDescending(c => c.FirstName) : query.OrderBy(c => c.FirstName)
            };
        }

        private async Task AddUserRolesAsync(Guid userId, List<Guid> roleIds)
        {
            foreach (var roleId in roleIds)
            {
                // Verify the role exists
                if (!await _context.Roles.AnyAsync(r => r.Id == roleId))
                {
                    _logger.LogWarning("Attempted to assign non-existent role ID {RoleId} to user {UserId}",
                        roleId, userId);
                    continue;
                }

                await _context.UserRoles!.AddAsync(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.GetCurrentUsername ?? "System"
                });
            }

            await _context.SaveChangesAsync();
        }

        private bool IsValidImageFile(IFormFile file)
        {
            // Check file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return false;

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };

            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            return true;
        }

        private void DeleteProfileImage(string profileImagePath)
        {
            try
            {
                // Clean up the path to ensure it's a relative path
                var relativePath = profileImagePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                // Check if file exists
                if (File.Exists(fullPath))
                {
                    // Delete the file
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted profile image: {Path}", fullPath);
                }
                else
                {
                    _logger.LogWarning("Could not delete profile image: {Path} (file not found)", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile image: {Path}", profileImagePath);
            }
        }

        private string GenerateRandomPassword(int length = 12)
        {
            // Ensure sufficient entropy with more characters
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";
            const string special = "!@#$%^&*()_-+=<>?";

            // Combine all character sets
            var allChars = lowerCase + upperCase + numbers + special;

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            var result = new StringBuilder(length);

            // Ensure at least one character from each set
            result.Append(lowerCase[bytes[0] % lowerCase.Length]);
            result.Append(upperCase[bytes[1] % upperCase.Length]);
            result.Append(numbers[bytes[2] % numbers.Length]);
            result.Append(special[bytes[3] % special.Length]);

            // Fill the rest randomly
            for (int i = 4; i < length; i++)
            {
                result.Append(allChars[bytes[i] % allChars.Length]);
            }

            // Shuffle the string
            return new string(result.ToString().ToCharArray().OrderBy(s => Guid.NewGuid()).ToArray());
        }

        #endregion
    }
}