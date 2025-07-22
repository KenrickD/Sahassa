using Microsoft.AspNetCore.Authorization;

namespace WMS.Application.Authorization
{
    public class AuthorizeClientAttribute : AuthorizeAttribute
    {
        public AuthorizeClientAttribute(string clientId)
        {
            Policy = $"ClientAccess_{clientId}";
        }
    }
}