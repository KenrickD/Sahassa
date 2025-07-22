using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.Application.Authorization.Requirements;
using WMS.Infrastructure.Data;

namespace WMS.Application.Authorization.Handlers
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly AppDbContext _context;

        public PermissionHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return;
            }

            // Check if user has the permission directly
            var hasDirectPermission = await _context.UserPermissions
                .AnyAsync(up => up.UserId == userId &&
                               up.Permission.Name == requirement.Permission);

            if (hasDirectPermission)
            {
                context.Succeed(requirement);
                return;
            }

            // Check if user has the permission through roles
            var hasRolePermission = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.RolePermissions,
                      ur => ur.RoleId,
                      rp => rp.RoleId,
                      (ur, rp) => rp)
                .AnyAsync(rp => rp.Permission.Name == requirement.Permission);

            if (hasRolePermission)
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}