using Microsoft.AspNetCore.Authorization;

namespace WMS.Application.Authorization
{
    public class AuthorizePermissionAttribute : AuthorizeAttribute
    {
        public AuthorizePermissionAttribute(string permission)
        {
            Policy = $"Permission_{permission}";
        }
    }
}