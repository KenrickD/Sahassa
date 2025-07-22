using Microsoft.AspNetCore.Http;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Services;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Models;
using WMS.WebApp.Models.Permissions;
using WMS.WebApp.Models.Users;

namespace WMS.Application.Interfaces
{
    public interface IUserService
    {
        Task<PaginatedResult<UserDto>> GetPaginatedUsers(string searchTerm, int skip, int take, int sortColumn, bool sortAscending);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto?> GetUserByUsernameAsync(string username);
        Task<UserDto> CreateUserAsync(UserCreateDto userDtoe);
        Task<UserDto> UpdateUserAsync(UserUpdateDto userDto);
        Task DeleteUserAsync(Guid id);
        Task<List<Role>> GetUserRolesAsync(Guid userId);
        Task<string> UploadProfileImageAsync(IFormFile file, string? existingImagePath = null);
        string GetProfileImageUrl(string? profileImagePath);
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string confirmationPassword);
        Task<string> ResetPasswordAsync(Guid userId);
        Task<bool> IsUsernameAvailableAsync(string username, Guid? excludeUserId = null);
        Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null);
        // User Role Management
        Task<UserRolesDataResultDto> GetUserRolesDataAsync(Guid userId);
        Task<ServiceResultDto> SaveUserRoleChangesAsync(Guid userId, List<RoleChangeRequestDto> changes, string currentUserId);
        // User Permission Management
        Task<UserPermissionsDataResultDto> GetUserPermissionsDataAsync(Guid userId);
        Task<ServiceResultDto> SaveUserPermissionChangesAsync(
            Guid userId,
            List<PermissionChangeRequestDto> changes,
            string currentUserId);
    }
}