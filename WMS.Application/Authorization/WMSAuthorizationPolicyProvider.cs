using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WMS.Application.Authorization.Requirements;

namespace WMS.Application.Authorization
{
    public class WMSAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public WMSAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check for warehouse access policies
            if (policyName.StartsWith("WarehouseAccess_"))
            {
                var warehouseIdString = policyName.Substring("WarehouseAccess_".Length);
                if (Guid.TryParse(warehouseIdString, out var warehouseId))
                {
                    var policy = new AuthorizationPolicyBuilder();
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new WarehouseAccessRequirement(warehouseId));
                    return policy.Build();
                }
            }

            // Check for client access policies
            if (policyName.StartsWith("ClientAccess_"))
            {
                var clientIdString = policyName.Substring("ClientAccess_".Length);
                if (Guid.TryParse(clientIdString, out var clientId))
                {
                    var policy = new AuthorizationPolicyBuilder();
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new ClientAccessRequirement(clientId));
                    return policy.Build();
                }
            }

            // Check for permission policies
            if (policyName.StartsWith("Permission_"))
            {
                var permission = policyName.Substring("Permission_".Length);
                var policy = new AuthorizationPolicyBuilder();
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new PermissionRequirement(permission));
                return policy.Build();
            }

            // Default to the base implementation
            return await base.GetPolicyAsync(policyName);
        }
    }
}