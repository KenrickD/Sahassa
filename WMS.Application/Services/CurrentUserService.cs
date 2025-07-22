using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WMS.Domain.Interfaces;

namespace WMS.Application.Services
{
    /// <summary>
    /// Implementation of the current user service
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Get the current user's username
        /// </summary>
        public string GetCurrentUsername =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        /// <summary>
        /// Get the current user's ID
        /// </summary>
        public string UserId =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

        /// <summary>
        /// Get the current user's warehouse ID
        /// </summary>
        public Guid CurrentWarehouseId
        {
            get
            {
                var warehouseIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("warehouse_id");
                return !string.IsNullOrEmpty(warehouseIdClaim) && Guid.TryParse(warehouseIdClaim, out var warehouseId)
                    ? warehouseId
                    : Guid.Empty;
            }
        }

        /// <summary>
        /// Get the current user's client ID (null for system users)
        /// </summary>
        public Guid? CurrentClientId
        {
            get
            {
                var clientIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("client_id");
                return !string.IsNullOrEmpty(clientIdClaim) && Guid.TryParse(clientIdClaim, out var clientId)
                    ? clientId
                    : null;
            }
        }

        /// <summary>
        /// Check if the user is authenticated
        /// </summary>
        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// Check if the user has a specific role
        /// </summary>
        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// Check if the user has a specific permission
        /// </summary>
        public bool HasPermission(string permission)
        {
            if (IsInRole("SystemAdmin") == true)
                return true;

            // Implementation option 1: Check the Permission claim directly
            var hasPermissionClaim = _httpContextAccessor.HttpContext?.User?.HasClaim(c =>
                c.Type == "Permission" && c.Value == permission);

            if (hasPermissionClaim == true)
                return true;

            // Implementation option 2: Check the custom UserClaims
            var userClaims = _httpContextAccessor.HttpContext?.User?.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value);

            return userClaims != null && userClaims.Contains(permission);
        }
    }
}