using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.Application.Authorization.Requirements;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Data;

namespace WMS.Application.Authorization.Handlers
{
    public class WarehouseAccessHandler : AuthorizationHandler<WarehouseAccessRequirement>
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public WarehouseAccessHandler(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            WarehouseAccessRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return;
            }

            // Check for system admin
            if (requirement.AllowSystemAdmin && context.User.IsInRole("SystemAdmin"))
            {
                context.Succeed(requirement);
                return;
            }

            var user = await _context.Users
                .Include(u => u.Warehouse)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                return;
            }

            // Check if user belongs to the required warehouse
            if (user.WarehouseId == requirement.WarehouseId)
            {
                context.Succeed(requirement);
                return;
            }

            // Check for warehouse manager with cross-warehouse permissions
            if (context.User.IsInRole("WarehouseManager"))
            {
                var hasCrossWarehousePermission = await _context.UserPermissions
                    .AnyAsync(up => up.UserId == userId &&
                                   up.Permission.Name == "Warehouse.AccessAll");

                if (hasCrossWarehousePermission)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}
