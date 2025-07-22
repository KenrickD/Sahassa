using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WMS.WebApp.Models;

namespace WMS.WebApp.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Error/Error/{statusCode:int}")]
        [Route("Error/Error")]
        public IActionResult Error(int? statusCode = null)
        {
            // Get correlation ID - fallback chain
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString()
                               ?? HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                               ?? Guid.NewGuid().ToString();

            var username = HttpContext.Items["Username"]?.ToString() ?? "Anonymous";

            // Create error model
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                CorrelationId = correlationId,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow,
                RequestPath = HttpContext.Request.Path,
                UserAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            };

            // SAFE LOGGING - Use try-catch to prevent error page from failing
            try
            {
                _logger.LogError("Error page accessed - StatusCode: {StatusCode}, CorrelationId: {CorrelationId}, " +
                               "Username: {Username}, RequestPath: {RequestPath}",
                               statusCode, correlationId, username, errorViewModel.RequestPath);
            }
            catch (Exception logEx)
            {
                // If logging fails, write to console as last resort
                Console.WriteLine($"Logging failed in Error action: {logEx.Message}");
            }

            // Set view properties based on status code
            ConfigureErrorView(statusCode);
            switch (statusCode)
            {
                case 404:
                    return View("NotFound", errorViewModel);
                case 403:
                    return View("Forbidden", errorViewModel);
                default:
                    return View("Error", errorViewModel);

            }
            //return View("Error", errorViewModel);
        }

        private void ConfigureErrorView(int? statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Page Not Found";
                    ViewBag.ErrorDescription = "The page you're looking for doesn't exist or has been moved.";
                    ViewBag.ErrorIcon = "🔍";
                    ViewBag.ShowRetryButton = false;
                    break;

                case 403:
                    ViewBag.ErrorMessage = "Access Denied";
                    ViewBag.ErrorDescription = "You don't have permission to access this resource.";
                    ViewBag.ErrorIcon = "🔒";
                    ViewBag.ShowRetryButton = false;
                    break;

                case 500:
                case null:
                    ViewBag.ErrorMessage = "Something Went Wrong";
                    ViewBag.ErrorDescription = "We're experiencing technical difficulties. Our team has been notified and is working to resolve the issue.";
                    ViewBag.ErrorIcon = "⚠️";
                    ViewBag.ShowRetryButton = true;
                    ViewBag.ShowSupportInfo = true;
                    break;

                case 502:
                case 503:
                case 504:
                    ViewBag.ErrorMessage = "Service Temporarily Unavailable";
                    ViewBag.ErrorDescription = "The service is temporarily unavailable. Please try again in a few minutes.";
                    ViewBag.ErrorIcon = "🔧";
                    ViewBag.ShowRetryButton = true;
                    break;

                default:
                    ViewBag.ErrorMessage = "An Error Occurred";
                    ViewBag.ErrorDescription = "An unexpected error occurred. Please try again or contact support if the problem persists.";
                    ViewBag.ErrorIcon = "❌";
                    ViewBag.ShowRetryButton = true;
                    break;
            }
        }
    }
}