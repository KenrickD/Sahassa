using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;

namespace WMS.WebAPI.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
            var stopwatch = Stopwatch.StartNew();

            // Add correlation ID to response headers
            context.Response.Headers.Add("X-Correlation-ID", correlationId);

            // Push properties to log context
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
            using (LogContext.PushProperty("IpAddress", GetClientIpAddress(context)))
            using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].ToString()))
            using (LogContext.PushProperty("UserId", GetUserId(context)))
            using (LogContext.PushProperty("UserEmail", GetUserEmail(context)))
            {
                // Log request start
                _logger.LogInformation("HTTP Request Started: {Method} {Scheme}://{Host}{Path}{QueryString}",
                    context.Request.Method,
                    context.Request.Scheme,
                    context.Request.Host,
                    context.Request.Path,
                    context.Request.QueryString);

                try
                {
                    await _next(context);
                }
                finally
                {
                    stopwatch.Stop();

                    // Determine log level based on status code
                    var logLevel = context.Response.StatusCode switch
                    {
                        >= 500 => LogLevel.Error,
                        >= 400 => LogLevel.Warning,
                        _ => LogLevel.Information
                    };

                    // Log response
                    _logger.Log(logLevel, "HTTP Request Completed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);

                    // Log slow requests
                    if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds
                    {
                        _logger.LogWarning("Slow Request Detected: {Method} {Path} took {ElapsedMs}ms",
                            context.Request.Method,
                            context.Request.Path,
                            stopwatch.ElapsedMilliseconds);
                    }

                    // Log high memory usage (if available)
                    var memoryUsage = GC.GetTotalMemory(false);
                    if (memoryUsage > 100_000_000) // 100MB
                    {
                        _logger.LogWarning("High Memory Usage: {MemoryBytes} bytes during request {Method} {Path}",
                            memoryUsage, context.Request.Method, context.Request.Path);
                    }
                }
            }
        }

        private static string GetUserId(HttpContext context)
        {
            return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        }

        private static string GetUserEmail(HttpContext context)
        {
            return context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
                return context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

            if (context.Request.Headers.ContainsKey("X-Real-IP"))
                return context.Request.Headers["X-Real-IP"].ToString();

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}