using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Data;

namespace WMS.Identity.Requirements
{
    // Resource Access Requirement (for entity-level authorization)
    public class ResourceAccessRequirement : IAuthorizationRequirement
    {
        public string ResourceType { get; }
        public string Operation { get; } // Create, Read, Update, Delete

        public ResourceAccessRequirement(string resourceType, string operation)
        {
            ResourceType = resourceType;
            Operation = operation;
        }
    }

    // Resource Access Handler
    public class ResourceAccessHandler : AuthorizationHandler<ResourceAccessRequirement>
    {
        private readonly AppDbContext _dbContext;
        private readonly ITenantService _tenantService;

        public ResourceAccessHandler(AppDbContext dbContext, ITenantService tenantService)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceAccessRequirement requirement)
        {
            // System admins have access to everything
            if (_tenantService.IsSystemAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // Get the user's roles and permissions
            var userId = context.User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            // Check if the user has the required permission for this resource and operation
            var permissionName = $"{requirement.ResourceType}.{requirement.Operation}";

            var hasPermission = await (
                from userRole in _dbContext.UserRoles
                join rolePermission in _dbContext.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                join permission in _dbContext.Permissions on rolePermission.PermissionId equals permission.Id
                where userRole.UserId == Guid.Parse(userId) && permission.Name == permissionName
                select permission
            ).AnyAsync();

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}
