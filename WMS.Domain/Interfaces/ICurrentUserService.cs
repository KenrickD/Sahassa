using System.Security.Claims;

namespace WMS.Domain.Interfaces
{
    /// <summary>
    /// Service for accessing the current authenticated user
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Get the current user's username
        /// </summary>
        string GetCurrentUsername { get; }

        /// <summary>
        /// Get the current user's ID
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Get the current user's warehouse ID
        /// </summary>
        Guid CurrentWarehouseId { get; }

        /// <summary>
        /// Get the current user's client ID (null for system users)
        /// </summary>
        Guid? CurrentClientId { get; }

        /// <summary>
        /// Check if the user is authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Check if the user has a specific role
        /// </summary>
        bool IsInRole(string role);

        /// <summary>
        /// Check if the user has a specific permission
        /// </summary>
        bool HasPermission(string permission);
    }
}