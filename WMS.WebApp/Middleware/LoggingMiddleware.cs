using System.Security.Claims;

namespace WMS.WebApp.Middlewares
{
    // Custom logging middleware
    public class LoggingMiddleware : IMiddleware
    {
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // Don't log requests for static files
            if (context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js") ||
                context.Request.Path.StartsWithSegments("/lib") ||
                context.Request.Path.StartsWithSegments("/images"))
            {
                await next(context);
                return;
            }

            // Start stopwatch for request duration
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Capture original body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                // Set up logging context
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["UserId"] = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                    ["UserName"] = context.User?.FindFirstValue(ClaimTypes.Name) ?? "anonymous",
                    ["RequestId"] = context.TraceIdentifier,
                    ["IPAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                }))
                {
                    // Log the incoming request (but don't log the body/form data for privacy)
                    _logger.LogInformation(
                        "HTTP {Method} {Path}{QueryString} started - User: {User}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.QueryString,
                        context.User?.Identity?.Name ?? "anonymous"
                    );

                    // Call the next middleware
                    await next(context);

                    // Log the response
                    sw.Stop();
                    _logger.LogInformation(
                        "HTTP {Method} {Path}{QueryString} completed - Status: {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.QueryString,
                        context.Response.StatusCode,
                        sw.ElapsedMilliseconds
                    );
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(
                    ex,
                    "HTTP {Method} {Path}{QueryString} failed after {ElapsedMs}ms - {ExceptionMessage}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString,
                    sw.ElapsedMilliseconds,
                    ex.Message
                );

                throw; // Re-throw to let the exception handler middleware handle it
            }
        }
    }
}
