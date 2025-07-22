using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Common;
using WMS.Domain.Models;

namespace WMS.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto> LoginAsync(LoginDto loginDto);
        Task<AuthResultDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResultDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<bool> ActivateUserAsync(Guid userId);
        Task<bool> DeactivateUserAsync(Guid userId);
        Task<bool> AssignRolesToUserAsync(Guid userId, List<Guid> roleIds);
        Task<bool> RemoveRolesFromUserAsync(Guid userId, List<Guid> roleIds);
        Task<bool> ForgotPasswordAsync(string email);
        //API region
        Task<ApiResponseDto<LoginResponseDto>> AuthenticateAPIAsync(LoginRequestDto request, string ipAddress);
        Task<ApiResponseDto<LoginResponseDto>> RefreshTokenAPIAsync(RefreshTokenDto request, string ipAddress);
        Task<ApiResponseDto<bool>> RevokeTokenAPIAsync(string token, string ipAddress);
        Task<ApiResponseDto<bool>> LogoutAPIAsync(string userId);
        Task<bool> ValidatePasswordAsync(string email, string password);
        Task<List<string>> GetUserPermissionsAsync(Guid userId);
    }
}