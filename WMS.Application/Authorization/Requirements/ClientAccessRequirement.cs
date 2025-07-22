using Microsoft.AspNetCore.Authorization;

namespace WMS.Application.Authorization.Requirements
{
    public class ClientAccessRequirement : IAuthorizationRequirement
    {
        public Guid ClientId { get; }
        public bool AllowWarehouseManagers { get; set; } = true;

        public ClientAccessRequirement(Guid clientId)
        {
            ClientId = clientId;
        }
    }
}