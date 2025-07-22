using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;

namespace WMS.WebApp.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;

        public SidebarViewComponent(IUserService userService, ICurrentUserService currentUserService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentUserId = _currentUserService.UserId;

            if (Guid.TryParse(currentUserId, out var userId))
            {
                var currentUser = await _userService.GetUserByIdAsync(userId);
                return View(currentUser);
            }

            return View(new UserDto());
        }
    }
}
