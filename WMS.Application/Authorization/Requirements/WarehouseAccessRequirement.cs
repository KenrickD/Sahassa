using Microsoft.AspNetCore.Authorization;

namespace WMS.Application.Authorization.Requirements
{
    public class WarehouseAccessRequirement : IAuthorizationRequirement
    {
        public Guid WarehouseId { get; }
        public bool AllowSystemAdmin { get; set; } = true;

        public WarehouseAccessRequirement(Guid warehouseId)
        {
            WarehouseId = warehouseId;
        }
    }
}