using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.Application.Authorization.Requirements;
using WMS.Infrastructure.Data;

namespace WMS.Application.Authorization.Handlers
{
    public class ClientAccessHandler : AuthorizationHandler<ClientAccessRequirement>
    {
        private readonly AppDbContext _context;

        public ClientAccessHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ClientAccessRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return;
            }

            // System admin has access to all clients
            if (context.User.IsInRole("SystemAdmin"))
            {
                context.Succeed(requirement);
                return;
            }

            var user = await _context.Users
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                return;
            }

            // Check if user is associated with the client
            if (user.ClientId == requirement.ClientId)
            {
                context.Succeed(requirement);
                return;
            }

            // Check for warehouse managers with permission to access client data
            if (requirement.AllowWarehouseManagers && context.User.IsInRole("WarehouseManager"))
            {
                var hasClientAccess = await _context.UserPermissions
                    .AnyAsync(up => up.UserId == userId &&
                                   up.Permission.Name == "Client.AccessAll");

                if (hasClientAccess)
                {
                    context.Succeed(requirement);
                    return;
                }

                // Check if the client belongs to the user's warehouse
                var clientInWarehouse = await _context.Clients
                    .AnyAsync(c => c.Id == requirement.ClientId &&
                                  c.WarehouseId == user.WarehouseId);

                if (clientInWarehouse)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}
