using Microsoft.AspNetCore.Mvc;
using WMS.Application.Helpers;

namespace WMS.WebApp.ViewComponents
{
    public class NavbarViewComponent : ViewComponent
    {
        private readonly MemoryHelper _memoryHelper;

        public NavbarViewComponent(MemoryHelper memoryHelper)
        {
            _memoryHelper = memoryHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            _memoryHelper.ClearCache();
            var userProfile = await _memoryHelper.GetUserProfileAsync();
            return View(userProfile); // Pass user profile to the view
        }
    }
}
