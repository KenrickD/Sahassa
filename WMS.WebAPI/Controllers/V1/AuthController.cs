using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Common;

namespace WMS.WebAPI.Controllers.V1
{
    /// <summary>
    /// Authentication endpoints for user login, logout, and token management
    /// </summary>
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT tokens
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication response with tokens and user info</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="429">Too many requests</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<ApiResponseDto<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            var correlationId = Guid.NewGuid().ToString();
            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            using var scope = _logger.BeginScope("Auth.Login - CorrelationId: {CorrelationId}, Email: {Email}, IP: {IpAddress}",
                correlationId, request.Email, ipAddress);

            _logger.LogInformation("Login attempt started for user: {Email} from IP: {IpAddress} with UserAgent: {UserAgent}",
                request.Email, ipAddress, userAgent);

            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Login validation failed for user: {Email}. Errors: {Errors}",
                        request.Email, string.Join(", ", errors));

                    return BadRequest(ApiResponseDto<object>.ErrorResult("Invalid input data", errors));
                }

                // Authenticate user
                var result = await _authService.AuthenticateAPIAsync(request, ipAddress);

                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for user: {Email} from IP: {IpAddress}. Reason: {Reason}",
                        request.Email, ipAddress, result.Message);

                    return Unauthorized(result);
                }

                _logger.LogInformation("Login successful for user: {Email} (UserId: {UserId}) from IP: {IpAddress}",
                    request.Email, result.Data!.User.Id, ipAddress);

                // Set refresh token cookie
                SetRefreshTokenCookie(result.Data.RefreshToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for user: {Email} from IP: {IpAddress}. CorrelationId: {CorrelationId}",
                    request.Email, ipAddress, correlationId);

                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New access token and refresh token</returns>
        /// <response code="200">Token refreshed successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Invalid or expired refresh token</response>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponseDto<LoginResponseDto>>> RefreshToken([FromBody] RefreshTokenDto request)
        {
            var correlationId = Guid.NewGuid().ToString();
            var ipAddress = GetClientIpAddress();

            using var scope = _logger.BeginScope("Auth.RefreshToken - CorrelationId: {CorrelationId}, IP: {IpAddress}",
                correlationId, ipAddress);

            _logger.LogInformation("Token refresh attempt from IP: {IpAddress}", ipAddress);

            try
            {
                // Get refresh token from cookie if not in request body
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    request.RefreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
                }

                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    _logger.LogWarning("Token refresh failed - No refresh token provided from IP: {IpAddress}", ipAddress);
                    return BadRequest(ApiResponseDto<object>.ErrorResult("Refresh token is required"));
                }

                var result = await _authService.RefreshTokenAPIAsync(request, ipAddress);

                if (!result.Success)
                {
                    _logger.LogWarning("Token refresh failed from IP: {IpAddress}. Reason: {Reason}",
                        ipAddress, result.Message);
                    return Unauthorized(result);
                }

                _logger.LogInformation("Token refresh successful for user: {UserId} from IP: {IpAddress}",
                    result.Data!.User.Id, ipAddress);

                SetRefreshTokenCookie(result.Data.RefreshToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error from IP: {IpAddress}. CorrelationId: {CorrelationId}",
                    ipAddress, correlationId);

                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Logout user and revoke all refresh tokens
        /// </summary>
        /// <returns>Success status</returns>
        /// <response code="200">Logout successful</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponseDto<bool>>> Logout()
        {
            var correlationId = Guid.NewGuid().ToString();
            var userId = GetCurrentUserId();
            var userEmail = GetCurrentUserEmail();
            var ipAddress = GetClientIpAddress();

            using var scope = _logger.BeginScope("Auth.Logout - CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, userId);

            _logger.LogInformation("Logout initiated for user: {UserId} ({Email}) from IP: {IpAddress}",
                userId, userEmail, ipAddress);

            try
            {
                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("Logout failed - Invalid user ID from IP: {IpAddress}", ipAddress);
                    return Unauthorized(ApiResponseDto<object>.ErrorResult("Invalid user"));
                }

                var result = await _authService.LogoutAPIAsync(userId.ToString());

                // Clear refresh token cookie
                ClearRefreshTokenCookie();

                _logger.LogInformation("Logout successful for user: {UserId} ({Email}) from IP: {IpAddress}",
                    userId, userEmail, ipAddress);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error for user: {UserId} from IP: {IpAddress}. CorrelationId: {CorrelationId}",
                    userId, ipAddress, correlationId);

                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Get current authenticated user profile
        /// </summary>
        /// <returns>Current user information</returns>
        /// <response code="200">User profile retrieved successfully</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponseDto<UserProfileDto>>> GetProfile()
        {
            var correlationId = Guid.NewGuid().ToString();
            var userId = GetCurrentUserId();
            var userEmail = GetCurrentUserEmail();

            using var scope = _logger.BeginScope("Auth.GetProfile - CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, userId);

            _logger.LogInformation("Profile request for user: {UserId} ({Email})", userId, userEmail);

            try
            {
                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("Profile request failed - Invalid user ID");
                    return Unauthorized(ApiResponseDto<object>.ErrorResult("Invalid user"));
                }

                var profile = new UserProfileDto
                {
                    Id = userId,
                    Email = userEmail,
                    FirstName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty,
                    LastName = User.FindFirst(ClaimTypes.Surname)?.Value,
                    Username = GetCurrentUserName(),
                    IsActive = true,
                    Roles = GetUserRoles(),
                    ClientId = GetCurrentClientId(),
                    ClientName = GetCurrentClientName(),
                    WarehouseId = GetCurrentWarehouseId(),
                    WarehouseCode = GetCurrentWarehouseCode(),
                    WarehouseName = GetCurrentWarehouseName(),
                };

                _logger.LogInformation("Profile retrieved successfully for user: {UserId}", userId);

                return Ok(ApiResponseDto<UserProfileDto>.SuccessResult(profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile error for user: {UserId}. CorrelationId: {CorrelationId}",
                    userId, correlationId);

                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Revoke a specific refresh token
        /// </summary>
        /// <param name="request">Token to revoke</param>
        /// <returns>Success status</returns>
        /// <response code="200">Token revoked successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User not authenticated</response>
        [HttpPost("revoke-token")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseDto<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponseDto<bool>>> RevokeToken([FromBody] RevokeTokenRequestDto request)
        {
            var correlationId = Guid.NewGuid().ToString();
            var userId = GetCurrentUserId();
            var ipAddress = GetClientIpAddress();

            using var scope = _logger.BeginScope("Auth.RevokeToken - CorrelationId: {CorrelationId}, UserId: {UserId}",
                correlationId, userId);

            _logger.LogInformation("Token revocation requested by user: {UserId} from IP: {IpAddress}",
                userId, ipAddress);

            try
            {
                // Get token from cookie if not in request body
                var token = request.Token ?? Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token revocation failed - No token provided by user: {UserId}", userId);
                    return BadRequest(ApiResponseDto<object>.ErrorResult("Token is required"));
                }

                var result = await _authService.RevokeTokenAPIAsync(token, ipAddress);

                if (!result.Success)
                {
                    _logger.LogWarning("Token revocation failed for user: {UserId}. Reason: {Reason}",
                        userId, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Token revoked successfully for user: {UserId}", userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation error for user: {UserId}. CorrelationId: {CorrelationId}",
                    userId, correlationId);

                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }
    }
}
