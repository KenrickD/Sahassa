using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;

namespace WMS.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly AuthSettings _authSettings;
        private readonly JWTHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            ITokenService tokenService,
            IPasswordHasher<User> passwordHasher,
            IOptions<AuthSettings> authSettings,
            IConfiguration configuration,
            JWTHelper jWTHelper,
            ILogger<AuthService> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _authSettings = authSettings.Value;
            _configuration = configuration;
            _jwtHelper = jWTHelper;
            _logger = logger;
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .Include(u => u.Warehouse)
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

            if (user == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid username or password" }
                };
            }

            if (!user.IsActive)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Account is disabled" }
                };
            }

            if (user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Account is locked out" }
                };
            }

            // Verify warehouse access if specified
            if (loginDto.WarehouseId.HasValue && user.WarehouseId != loginDto.WarehouseId)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "You don't have access to this warehouse" }
                };
            }

            var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);

            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                // Increment failed login attempts
                user.AccessFailedCount++;

                if (user.AccessFailedCount >= _authSettings.MaxFailedAccessAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(_authSettings.LockoutMinutes);
                    user.LockoutEnabled = true;
                }

                await _context.SaveChangesAsync();

                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid username or password" }
                };
            }

            // Reset failed login attempts
            user.AccessFailedCount = 0;
            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GenerateAuthResultDtoAsync(user);
        }

        public async Task<AuthResultDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Username already exists" }
                };
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Email already exists" }
                };
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = registerDto.Username,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                WarehouseId = registerDto.WarehouseId,
                ClientId = registerDto.ClientId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);
            user.SecurityStamp = Guid.NewGuid().ToString();

            _context.Users.Add(user);

            // Assign roles
            if (registerDto.RoleIds != null && registerDto.RoleIds.Any())
            {
                foreach (var roleId in registerDto.RoleIds)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = roleId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    });
                }
            }
            else
            {
                // Assign default role
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "WarehouseUser");
                if (defaultRole != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = defaultRole.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Load related data
            user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .Include(u => u.Warehouse)
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            return await GenerateAuthResultDtoAsync(user!);
        }

        public async Task<AuthResultDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var principal = await _tokenService.ValidateAccessTokenAsync(refreshTokenDto.AccessToken, false);
            if (principal == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid access token" }
                };
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid token claims" }
                };
            }

            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserPermissions)
                        .ThenInclude(up => up.Permission)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Warehouse)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Client)
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken && rt.UserId == userId);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid refresh token" }
                };
            }

            // Replace old refresh token with a new one
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.ReplacedByToken = null; // Will be set below

            var user = refreshToken.User;
            await _context.SaveChangesAsync();

            return await GenerateAuthResultDtoAsync(user);
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || !token.IsActive)
            {
                return false;
            }

            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = "Revoked by user";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (passwordVerification == PasswordVerificationResult.Failed)
            {
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            // Revoke all active refresh tokens
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.ReasonRevoked = "Password changed";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .Include(u => u.Warehouse)
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> ActivateUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsActive)
            {
                return false;
            }

            user.IsActive = true;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = "System";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            user.IsActive = false;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = "System";

            // Revoke all active refresh tokens
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.ReasonRevoked = "User deactivated";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRolesToUserAsync(Guid userId, List<Guid> roleIds)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            foreach (var roleId in roleIds)
            {
                if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId))
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRolesFromUserAsync(Guid userId, List<Guid> roleIds)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
                .ToListAsync();

            _context.UserRoles.RemoveRange(userRoles);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                // Don't reveal that user doesn't exist for security
                return true;
            }

            // Generate new random password
            var newPassword = GenerateRandomPassword();

            // Hash and update password
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            await _context.SaveChangesAsync();

            // Send email with new password
            await SendPasswordResetEmailAsync(user.Email, newPassword);
            //await SendPasswordResetEmailAsync("arif@hsc.sg", newPassword);

            return true;
        }
        // Enhanced AuthService with comprehensive logging - no functionality changes

        public async Task<ApiResponseDto<LoginResponseDto>> AuthenticateAPIAsync(LoginRequestDto request, string ipAddress)
        {
            using var scope = _logger.BeginScope("AuthenticateAPI - Email: {Email}, IP: {IpAddress}", request.Email, ipAddress);

            _logger.LogInformation("Starting API authentication for user: {Email} from IP: {IpAddress}", request.Email, ipAddress);

            try
            {
                // Find user by email
                _logger.LogDebug("Searching for user by email: {Email}", request.Email);

                var user = await _context.Users
                    .Include(u => u.Client)
                    .Include(u => u.Warehouse)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null || !user.IsActive)
                {
                    if (user == null)
                    {
                        _logger.LogWarning("Authentication failed - User not found: {Email} from IP: {IpAddress}", request.Email, ipAddress);
                        await LogSecurityEventAsync("API Authentication Failed - User Not Found", request.Email, ipAddress);
                    }
                    else
                    {
                        _logger.LogWarning("Authentication failed - User inactive: {Email} (UserId: {UserId}) from IP: {IpAddress}",
                            request.Email, user.Id, ipAddress);
                        await LogSecurityEventAsync("API Authentication Failed - User Inactive", request.Email, ipAddress, user.Id);
                    }
                    return ApiResponseDto<LoginResponseDto>.ErrorResult("Invalid email or password");
                }

                if (user.UserRoles == null || !user.UserRoles.Any(x => x.Role.Name == AppConsts.Roles.MOBILE_APP_USER))
                {
                    return ApiResponseDto<LoginResponseDto>.ErrorResult("User does not have the required Mobile App User role.");
                }

                _logger.LogInformation("User found: {UserId} ({Email}), Active: {IsActive}, Warehouse: {WarehouseId}",
                    user.Id, user.Email, user.IsActive, user.WarehouseId);

                // Validate password
                _logger.LogDebug("Validating password for user: {UserId}", user.Id);

                if (!await ValidatePasswordAsync(request.Email, request.Password))
                {
                    _logger.LogWarning("Authentication failed - Invalid password for user: {Email} (UserId: {UserId}) from IP: {IpAddress}",
                        request.Email, user.Id, ipAddress);
                    await LogSecurityEventAsync("API Authentication Failed - Invalid Password", request.Email, ipAddress, user.Id);
                    return ApiResponseDto<LoginResponseDto>.ErrorResult("Invalid email or password");
                }

                _logger.LogInformation("Password validation successful for user: {UserId}", user.Id);

                // Get user permissions and warehouse access
                _logger.LogDebug("Retrieving permissions for user: {UserId}", user.Id);
                var permissions = await GetUserPermissionsAsync(user.Id);
                _logger.LogInformation("Retrieved {PermissionCount} permissions for user: {UserId}", permissions.Count, user.Id);

                // Generate tokens
                _logger.LogDebug("Generating tokens for user: {UserId}", user.Id);
                var accessToken = _jwtHelper.GenerateAccessToken(user, permissions);
                var refreshToken = _jwtHelper.GenerateRefreshToken(ipAddress);
                _logger.LogInformation("Tokens generated successfully for user: {UserId}", user.Id);

                // Save refresh token to database
                user.RefreshTokens ??= new List<RefreshToken>();
                user.RefreshTokens.Add(refreshToken);

                // Remove old refresh tokens
                var oldTokenCountBefore = user.RefreshTokens.Count;
                RemoveOldRefreshTokens(user);
                var oldTokenCountAfter = user.RefreshTokens.Count;
                var removedTokensCount = oldTokenCountBefore - oldTokenCountAfter;

                if (removedTokensCount > 0)
                {
                    _logger.LogInformation("Removed {RemovedTokenCount} old refresh tokens for user: {UserId}",
                        removedTokensCount, user.Id);
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("Refresh token saved to database for user: {UserId}", user.Id);

                var response = new LoginResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:AccessTokenExpiration"])),
                    User = new UserProfileDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Username = user.Username,
                        IsActive = user.IsActive,
                        Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
                        ClientId = user.ClientId,
                        ClientName = user.Client?.Name,
                        WarehouseId = user.WarehouseId,
                        WarehouseCode = user.Warehouse == null ? "" : user.Warehouse.Code,
                        WarehouseName = user.Warehouse == null ? "" : user.Warehouse.Name,
                    },
                    Permissions = permissions
                };

                _logger.LogInformation("API authentication successful for user: {Email} (UserId: {UserId}) from IP: {IpAddress}. " +
                    "Client: {ClientName}, Warehouse: {WarehouseName}",
                    request.Email, user.Id, ipAddress, user.Client?.Name, user.Warehouse?.Name);

                await LogSecurityEventAsync("API Authentication Successful", request.Email, ipAddress, user.Id,
                    $"Client: {user.Client?.Name}, Warehouse: {user.Warehouse?.Name}");

                return ApiResponseDto<LoginResponseDto>.SuccessResult(response, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API authentication error for user: {Email} from IP: {IpAddress}", request.Email, ipAddress);
                await LogSecurityEventAsync("API Authentication Error", request.Email, ipAddress, null, ex.Message);

                return ApiResponseDto<LoginResponseDto>.ErrorResult("An error occurred during authentication",
                    new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponseDto<LoginResponseDto>> RefreshTokenAPIAsync(RefreshTokenDto request, string ipAddress)
        {
            using var scope = _logger.BeginScope("RefreshTokenAPI - IP: {IpAddress}", ipAddress);

            _logger.LogInformation("Starting API token refresh from IP: {IpAddress}", ipAddress);

            try
            {
                _logger.LogDebug("Looking up user by refresh token from IP: {IpAddress}", ipAddress);

                var user = await GetUserByRefreshTokenAsync(request.RefreshToken);
                if (user == null)
                {
                    _logger.LogWarning("Token refresh failed - Invalid refresh token from IP: {IpAddress}", ipAddress);
                    await LogSecurityEventAsync("API Token Refresh Failed - Invalid Token", "Unknown", ipAddress);
                    return ApiResponseDto<LoginResponseDto>.ErrorResult("Invalid refresh token");
                }

                _logger.LogInformation("Token refresh requested for user: {UserId} ({Email}) from IP: {IpAddress}",
                    user.Id, user.Email, ipAddress);

                var refreshToken = user.RefreshTokens?.Single(x => x.Token == request.RefreshToken);
                if (refreshToken == null || !refreshToken.IsActive)
                {
                    _logger.LogWarning("Token refresh failed - Token not active for user: {UserId} from IP: {IpAddress}",
                        user.Id, ipAddress);
                    await LogSecurityEventAsync("API Token Refresh Failed - Token Not Active", user.Email, ipAddress, user.Id);
                    return ApiResponseDto<LoginResponseDto>.ErrorResult("Invalid refresh token");
                }

                _logger.LogDebug("Refresh token validated for user: {UserId}, rotating token", user.Id);

                // Replace old refresh token with new one
                var newRefreshToken = RotateRefreshToken(refreshToken, ipAddress);
                user.RefreshTokens?.Add(newRefreshToken);

                var oldTokenCountBefore = user.RefreshTokens?.Count ?? 0;
                RemoveOldRefreshTokens(user);
                var oldTokenCountAfter = user.RefreshTokens?.Count ?? 0;
                var removedTokensCount = oldTokenCountBefore - oldTokenCountAfter;

                if (removedTokensCount > 0)
                {
                    _logger.LogInformation("Removed {RemovedTokenCount} old refresh tokens during refresh for user: {UserId}",
                        removedTokensCount, user.Id);
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("New refresh token saved for user: {UserId}", user.Id);

                // Generate new access token
                _logger.LogDebug("Generating new access token for user: {UserId}", user.Id);
                var permissions = await GetUserPermissionsAsync(user.Id);
                var accessToken = _jwtHelper.GenerateAccessToken(user, permissions);

                _logger.LogInformation("New access token generated for user: {UserId} with {PermissionCount} permissions",
                    user.Id, permissions.Count);

                var response = new LoginResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:AccessTokenExpiration"])),
                    User = new UserProfileDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Username = user.Username,
                        IsActive = user.IsActive,
                        Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
                        ClientId = user.ClientId,
                        ClientName = user.Client?.Name,
                        WarehouseId = user.WarehouseId,
                        WarehouseCode = user.Warehouse == null ? "" : user.Warehouse.Code,
                        WarehouseName = user.Warehouse == null ? "" : user.Warehouse.Name
                    },
                    Permissions = permissions,
                };

                _logger.LogInformation("API token refresh successful for user: {UserId} ({Email}) from IP: {IpAddress}",
                    user.Id, user.Email, ipAddress);

                await LogSecurityEventAsync("API Token Refresh Successful", user.Email, ipAddress, user.Id);

                return ApiResponseDto<LoginResponseDto>.SuccessResult(response, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API token refresh error from IP: {IpAddress}", ipAddress);
                await LogSecurityEventAsync("API Token Refresh Error", "Unknown", ipAddress, null, ex.Message);

                return ApiResponseDto<LoginResponseDto>.ErrorResult("An error occurred during token refresh",
                    new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponseDto<bool>> RevokeTokenAPIAsync(string token, string ipAddress)
        {
            using var scope = _logger.BeginScope("RevokeTokenAPI - IP: {IpAddress}", ipAddress);

            _logger.LogInformation("Starting API token revocation from IP: {IpAddress}", ipAddress);

            try
            {
                _logger.LogDebug("Looking up user by refresh token for revocation from IP: {IpAddress}", ipAddress);

                var user = await GetUserByRefreshTokenAsync(token);
                if (user == null)
                {
                    _logger.LogWarning("Token revocation failed - Invalid token from IP: {IpAddress}", ipAddress);
                    await LogSecurityEventAsync("API Token Revocation Failed - Invalid Token", "Unknown", ipAddress);
                    return ApiResponseDto<bool>.ErrorResult("Invalid token");
                }

                _logger.LogInformation("Token revocation requested for user: {UserId} ({Email}) from IP: {IpAddress}",
                    user.Id, user.Email, ipAddress);

                var refreshToken = user.RefreshTokens?.Single(x => x.Token == token);
                if (refreshToken == null || !refreshToken.IsActive)
                {
                    _logger.LogWarning("Token revocation failed - Token not active for user: {UserId} from IP: {IpAddress}",
                        user.Id, ipAddress);
                    await LogSecurityEventAsync("API Token Revocation Failed - Token Not Active", user.Email, ipAddress, user.Id);
                    return ApiResponseDto<bool>.ErrorResult("Invalid token");
                }

                _logger.LogDebug("Revoking refresh token for user: {UserId}", user.Id);

                // Revoke token
                RevokeRefreshToken(refreshToken, ipAddress);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token revoked successfully for user: {UserId} ({Email}) from IP: {IpAddress}",
                    user.Id, user.Email, ipAddress);

                await LogSecurityEventAsync("API Token Revocation Successful", user.Email, ipAddress, user.Id);

                return ApiResponseDto<bool>.SuccessResult(true, "Token revoked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API token revocation error from IP: {IpAddress}", ipAddress);
                await LogSecurityEventAsync("API Token Revocation Error", "Unknown", ipAddress, null, ex.Message);

                return ApiResponseDto<bool>.ErrorResult("An error occurred during token revocation",
                    new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponseDto<bool>> LogoutAPIAsync(string userId)
        {
            using var scope = _logger.BeginScope("LogoutAPI - UserId: {UserId}", userId);

            _logger.LogInformation("Starting API logout for user: {UserId}", userId);

            try
            {
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("API logout failed - Invalid user ID format: {UserId}", userId);
                    return ApiResponseDto<bool>.ErrorResult("Invalid user ID");
                }

                _logger.LogDebug("Looking up user for logout: {UserId}", userGuid);

                var user = await _context.Users
                    .Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.Id == userGuid);

                if (user == null)
                {
                    _logger.LogWarning("API logout failed - User not found: {UserId}", userGuid);
                    return ApiResponseDto<bool>.ErrorResult("User not found");
                }

                _logger.LogInformation("Processing logout for user: {UserId} ({Email})", user.Id, user.Email);

                if (user?.RefreshTokens != null)
                {
                    var activeTokens = user.RefreshTokens.Where(t => t.IsActive).ToList();
                    _logger.LogInformation("Found {ActiveTokenCount} active refresh tokens for user: {UserId}",
                        activeTokens.Count, user.Id);

                    foreach (var token in activeTokens)
                    {
                        _logger.LogDebug("Revoking refresh token for user: {UserId}", user.Id);
                        RevokeRefreshToken(token, "logout");
                    }

                    if (activeTokens.Any())
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Revoked {RevokedTokenCount} refresh tokens for user: {UserId}",
                            activeTokens.Count, user.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("No refresh tokens found for user: {UserId} during logout", user.Id);
                }

                _logger.LogInformation("API logout successful for user: {UserId} ({Email})", user.Id, user.Email);
                await LogSecurityEventAsync("API Logout Successful", user.Email, "localhost", user.Id);

                return ApiResponseDto<bool>.SuccessResult(true, "Logout successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API logout error for user: {UserId}", userId);
                await LogSecurityEventAsync("API Logout Error", "Unknown", "localhost", null, ex.Message);

                return ApiResponseDto<bool>.ErrorResult("An error occurred during logout",
                    new List<string> { ex.Message });
            }
        }
        public async Task<bool> ValidatePasswordAsync(string email, string password)
        {
            using var scope = _logger.BeginScope("ValidatePassword - Email: {Email}", email);

            _logger.LogDebug("Starting password validation for user: {Email}", email);

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user == null)
                {
                    _logger.LogDebug("Password validation failed - User not found: {Email}", email);
                    return false;
                }

                _logger.LogDebug("User found for password validation: {UserId}", user.Id);

                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                var isValid = result == PasswordVerificationResult.Success;

                if (isValid)
                {
                    _logger.LogDebug("Password validation successful for user: {Email} (UserId: {UserId})", email, user.Id);
                }
                else
                {
                    _logger.LogDebug("Password validation failed for user: {Email} (UserId: {UserId})", email, user.Id);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password validation error for user: {Email}", email);
                return false;
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
        {
            using var scope = _logger.BeginScope("GetUserPermissions - UserId: {UserId}", userId);

            _logger.LogDebug("Starting permission retrieval for user: {UserId}", userId);

            try
            {
                var permissions = new List<string>();

                // Get permissions from roles
                _logger.LogDebug("Retrieving role permissions for user: {UserId}", userId);
                var rolePermissions = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions!)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                permissions.AddRange(rolePermissions);
                _logger.LogDebug("Found {RolePermissionCount} role permissions for user: {UserId}",
                    rolePermissions.Count, userId);

                // Get direct user permissions
                _logger.LogDebug("Retrieving direct user permissions for user: {UserId}", userId);
                var userPermissions = await _context.UserPermissions
                    .Where(up => up.UserId == userId)
                    .Select(up => up.Permission.Name)
                    .ToListAsync();

                permissions.AddRange(userPermissions);
                _logger.LogDebug("Found {UserPermissionCount} direct user permissions for user: {UserId}",
                    userPermissions.Count, userId);

                var distinctPermissions = permissions.Distinct().ToList();

                _logger.LogInformation("Retrieved total {TotalPermissionCount} unique permissions for user: {UserId} " +
                    "({RolePermissionCount} from roles, {DirectPermissionCount} direct)",
                    distinctPermissions.Count, userId, rolePermissions.Count, userPermissions.Count);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Permissions for user {UserId}: [{Permissions}]",
                        userId, string.Join(", ", distinctPermissions));
                }

                return distinctPermissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for user: {UserId}", userId);
                return new List<string>();
            }
        }


        #region Private Methods
        // Helper method for security event logging
        private async Task LogSecurityEventAsync(string eventType, string email, string ipAddress, Guid? userId = null, string? details = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = eventType,
                    EntityName = "API Authentication",
                    EntityId = userId ?? Guid.Empty,
                    //ChangesJson = details ?? $"Email: {email}",
                    ChangesJson = details != null
                        ? JsonSerializer.Serialize(details)
                        : JsonSerializer.Serialize(new { Email = email }),
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    Username = email,
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Security event logged: {EventType} for email: {Email}, IP: {IpAddress}",
                    eventType, email, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType} for email: {Email}", eventType, email);
            }
        }

        private async Task<User?> GetUserByRefreshTokenAsync(string token)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.Client)
                .Include(u => u.Warehouse)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == token));
        }

        private RefreshToken RotateRefreshToken(RefreshToken refreshToken, string ipAddress)
        {
            var newRefreshToken = _jwtHelper.GenerateRefreshToken(ipAddress);
            RevokeRefreshToken(refreshToken, ipAddress, newRefreshToken.Token);
            return newRefreshToken;
        }

        private void RevokeRefreshToken(RefreshToken token, string ipAddress, string? replacedByToken = null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReplacedByToken = replacedByToken;
        }

        private void RemoveOldRefreshTokens(User user)
        {
            var refreshTokenTTL = Convert.ToDouble(_configuration["JWT:RefreshTokenTTL"]);
            if (user.RefreshTokens != null)
            {
                var expiredTokens = user.RefreshTokens
                    .Where(x => !x.IsActive && x.CreatedAt.AddDays(refreshTokenTTL) <= DateTime.UtcNow)
                    .ToList();

                foreach (var token in expiredTokens)
                {
                    user.RefreshTokens.Remove(token);
                }
            }
        }
        private async Task<AuthResultDto> GenerateAuthResultDtoAsync(User user)
        {
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, null);

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

            return new AuthResultDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                WarehouseId = user.WarehouseId,
                WarehouseName = user.Warehouse?.Name,
                ClientId = user.ClientId,
                ClientName = user.Client?.Name,
                Roles = roles,
                Permissions = permissions
            };
        }
        private string GenerateRandomPassword()
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var random = new Random();
            var password = new char[12]; // 12 character password

            // Ensure at least one of each required character type
            password[0] = upperCase[random.Next(upperCase.Length)];
            password[1] = lowerCase[random.Next(lowerCase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = specialChars[random.Next(specialChars.Length)];

            // Fill the rest randomly
            string allChars = upperCase + lowerCase + digits + specialChars;
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the password
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
        private async Task SendPasswordResetEmailAsync(string email, string newPassword)
        {
            var subject = "Your New Password - Warehouse Management System";
            var body = $@"
                    <h2>Password Reset</h2>
                    <p>Your password has been reset successfully.</p>
                    <p>Your new password is: <strong>{newPassword}</strong></p>
                    <p>Please login with this new password and change it to something you can remember.</p>
                    <br/>
                ";

            List<string> recipients = new List<string>();
            recipients.Add(email);

            await EmailHelper.SendEmailAsync(recipients, subject, body);
        }
        #endregion
    }
}
