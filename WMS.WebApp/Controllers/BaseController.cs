using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;

namespace WMS.WebApp.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ICurrentUserService _currentUserService;
        protected readonly IUserService _userService;

        public BaseController(ICurrentUserService currentUserService, IUserService userService)
        {
            _currentUserService = currentUserService;
            _userService = userService;
        }

        protected Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        protected Guid GetCurrentWarehouseId()
        {
            var warehouseIdClaim = User.FindFirstValue("WarehouseId");
            return Guid.TryParse(warehouseIdClaim, out var warehouseId) ? warehouseId : Guid.Empty;
        }

        protected Guid? GetCurrentClientId()
        {
            var clientIdClaim = User.FindFirstValue("ClientId");
            return Guid.TryParse(clientIdClaim, out var clientId) ? clientId : null;
        }

        protected bool HasPermission(string permission)
        {
            return User.HasClaim("Permission", permission);
        }

        protected bool IsInRole(string role)
        {
            return User.IsInRole(role);
        }
        protected async Task<UserDto> GetCurrentUserAsync()
        {
            var currentUserId = _currentUserService.UserId;

            if (Guid.TryParse(currentUserId, out var userId))
            {
                return await _userService.GetUserByIdAsync(userId);
            }

            return new UserDto();
        }
    }
}