using Microsoft.AspNetCore.Authorization;

namespace WMS.Application.Authorization
{
    public class AuthorizeWarehouseAttribute : AuthorizeAttribute
    {
        public AuthorizeWarehouseAttribute(string warehouseId)
        {
            Policy = $"WarehouseAccess_{warehouseId}";
        }
    }
}