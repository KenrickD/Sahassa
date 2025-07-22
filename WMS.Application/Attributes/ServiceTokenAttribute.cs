using Microsoft.AspNetCore.Authorization;

namespace WMS.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ServiceTokenAttribute : AuthorizeAttribute
    {
        public ServiceTokenAttribute(string? permission = null)
        {
            AuthenticationSchemes = "ServiceToken";

            if (!string.IsNullOrEmpty(permission))
            {
                Policy = $"ServiceToken.{permission}";
            }
        }
    }
}