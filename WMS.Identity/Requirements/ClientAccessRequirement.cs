using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Data;

namespace WMS.Identity.Requirements
{
    // Client Access Requirement
    public class ClientAccessRequirement : IAuthorizationRequirement
    {
    }

    // Client Access Handler
    public class ClientAccessHandler : AuthorizationHandler<ClientAccessRequirement>
    {
        private readonly AppDbContext _dbContext;
        private readonly ITenantService _tenantService;
        private readonly ICurrentUserService _currentUserService;

        public ClientAccessHandler(
            AppDbContext dbContext,
            ITenantService tenantService,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
            _currentUserService = currentUserService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ClientAccessRequirement requirement)
        {
            // System admins have access to everything
            if (_tenantService.IsSystemAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Get the resource ID from the request context
            var resourceId = context.Resource as Guid?;
            if (resourceId == null)
            {
                // If no specific resource is being accessed, check if the user has general client access
                var clientIdClaim = context.User.FindFirst("client_id")?.Value;
                if (!string.IsNullOrEmpty(clientIdClaim))
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // Warehouse managers have access to all clients in their warehouse
            if (_tenantService.IsWarehouseManager)
            {
                var clientBelongsToWarehouse = await _dbContext.Clients
                    .AnyAsync(c => c.Id == resourceId && c.WarehouseId == _tenantService.CurrentWarehouseId);

                if (clientBelongsToWarehouse)
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // Client users can only access their own client
            if (_tenantService.IsClientUser && _tenantService.CurrentClientId.HasValue)
            {
                if (resourceId.Value == _tenantService.CurrentClientId.Value)
                {
                    context.Succeed(requirement);
                }
                return;
            }
        }
    }
}
