using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.Interfaces;
using WMS.WebApp.Models;

namespace WMS.WebApp.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger, ICurrentUserService currentUserService, IUserService userService)
    : base(currentUserService, userService)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //var currentUser = await GetCurrentUserAsync();
            //return View(currentUser);

            //return RedirectToAction("Index", "LocationGrid");
            return View();
        }
        public IActionResult RenderNavbar()
        {
            return ViewComponent("Navbar");
        }
        [Authorize(Roles = "WarehouseManager,SystemAdmin")]
        public IActionResult Management()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}