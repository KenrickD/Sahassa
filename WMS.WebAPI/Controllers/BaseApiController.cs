using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WMS.WebAPI.Controllers
{
    /// <summary>
    /// Base controller for API endpoints with common functionality
    /// </summary>
    [ApiController]
    [Route("v1/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        protected string GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }

        protected string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }

        protected Guid GetCurrentWarehouseId()
        {
            var warehouseIdClaim = User.FindFirst("warehouse_id")?.Value;
            return Guid.TryParse(warehouseIdClaim, out var warehouseId) ? warehouseId : Guid.Empty;
        }
        protected string GetCurrentWarehouseCode()
        {
            return User.FindFirst("warehouse_code")?.Value ?? string.Empty;
        }
        protected string GetCurrentWarehouseName()
        {
            return User.FindFirst("warehouse_name")?.Value ?? string.Empty;
        }
        protected Guid? GetCurrentClientId()
        {
            var clientIdClaim = User.FindFirst("client_id")?.Value;
            return Guid.TryParse(clientIdClaim, out var clientId) ? clientId : null;
        }
        protected string GetCurrentClientName()
        {
            return User.FindFirst("client_name")?.Value ?? string.Empty;
        }
        protected List<string> GetUserPermissions()
        {
            return User.FindAll("permission").Select(c => c.Value).ToList();
        }

        protected List<string> GetUserRoles()
        {
            return User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }

        protected string GetClientIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

            if (Request.Headers.ContainsKey("X-Real-IP"))
                return Request.Headers["X-Real-IP"].ToString();

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        protected string GetUserAgent()
        {
            return Request.Headers["User-Agent"].ToString();
        }

        protected void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps,
                Path = "/api"
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        protected void ClearRefreshTokenCookie()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(-1),
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps,
                Path = "/api"
            };
            Response.Cookies.Append("refreshToken", "", cookieOptions);
        }
    }
}
