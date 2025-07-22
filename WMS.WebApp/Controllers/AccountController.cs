using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.Auth;
using WMS.Domain.DTOs.Users;
using WMS.WebApp.Models;

namespace WMS.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccountController> _logger;
        public AccountController(IAuthService authService, IMemoryCache cache, ILogger<AccountController> logger)
        {
            _authService = authService;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(SecureLoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var loginDto = new LoginDto
            {
                Email = model.Email,
                Password = model.Password
                //WarehouseId = model.WarehouseId
            };

            var result = await _authService.LoginAsync(loginDto);

            if (result.Success)
            {
                // Create claims for cookie authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                    new Claim(ClaimTypes.Name, result.Username),
                    new Claim(ClaimTypes.Email, result.Email),
                    new Claim("FullName", result.FullName),
                    new Claim("warehouse_id", result.WarehouseId.ToString())
                };

                if (result.ClientId.HasValue)
                {
                    claims.Add(new Claim("client_id", result.ClientId.Value.ToString()));
                }

                // Add roles
                foreach (var role in result.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Add permissions
                foreach (var permission in result.Permissions)
                {
                    claims.Add(new Claim("Permission", permission));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Store tokens in session for API calls if needed
                HttpContext.Session.SetString("AccessToken", result.AccessToken);
                HttpContext.Session.SetString("RefreshToken", result.RefreshToken);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Revoke refresh token if exists
            var refreshToken = HttpContext.Session.GetString("RefreshToken");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.RevokeTokenAsync(refreshToken);
            }

            // Clear session
            HttpContext.Session.Clear();

            // Sign out
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(SecureForgotPasswordViewModel model)
        {
            // Check if it's an AJAX request
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(errors);
                }
                return View(model);
            }

            try
            {
                await _authService.ForgotPasswordAsync(model.Email);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Error during password reset for email: {model.Email}");
            }

            if (isAjax)
            {
                return Ok(new { success = true });
            }

            // For non-AJAX requests
            TempData["EmailSent"] = true;
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            // Check if we're coming from a successful submission
            ViewBag.EmailSent = TempData["EmailSent"] as bool? ?? false;
            return View();
        }
    }
}