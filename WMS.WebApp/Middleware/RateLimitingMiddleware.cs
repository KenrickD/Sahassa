using System.Collections.Concurrent;
using System.Net;

namespace WMS.WebApp.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();

        // Rate limiting configuration
        private readonly int _maxRequestsPerMinute = 100; // General requests
        private readonly int _maxLoginAttemptsPerMinute = 5; // Login attempts
        private readonly int _maxApiRequestsPerMinute = 60; // API requests

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = GetClientIpAddress(context);
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Determine rate limit based on endpoint
            int maxRequests = GetMaxRequestsForEndpoint(path);

            if (IsRateLimited(clientIp, maxRequests))
            {
                _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}, Path: {Path}", clientIp, path);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Add("Retry-After", "60"); // Retry after 60 seconds

                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP first (for load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private int GetMaxRequestsForEndpoint(string path)
        {
            // Login endpoints - stricter limits
            if (path.Contains("/account/login") ||
                path.Contains("/auth/login") ||
                path.Contains("/account/forgotpassword"))
            {
                return _maxLoginAttemptsPerMinute;
            }

            // API endpoints
            if (path.StartsWith("/api/"))
            {
                return _maxApiRequestsPerMinute;
            }

            // General web requests
            return _maxRequestsPerMinute;
        }

        private bool IsRateLimited(string clientIp, int maxRequests)
        {
            var now = DateTime.UtcNow;

            _clients.AddOrUpdate(clientIp,
                new ClientRequestInfo { LastRequestTime = now, RequestCount = 1 },
                (key, existing) =>
                {
                    // Reset counter if more than a minute has passed
                    if (now.Subtract(existing.LastRequestTime).TotalMinutes >= 1)
                    {
                        existing.RequestCount = 1;
                        existing.LastRequestTime = now;
                    }
                    else
                    {
                        existing.RequestCount++;
                    }

                    return existing;
                });

            // Clean up old entries periodically
            CleanupOldEntries();

            return _clients[clientIp].RequestCount > maxRequests;
        }

        private void CleanupOldEntries()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5); // Remove entries older than 5 minutes
            var keysToRemove = _clients.Where(kvp => kvp.Value.LastRequestTime < cutoff)
                                     .Select(kvp => kvp.Key)
                                     .ToList();

            foreach (var key in keysToRemove)
            {
                _clients.TryRemove(key, out _);
            }
        }

        private class ClientRequestInfo
        {
            public DateTime LastRequestTime { get; set; }
            public int RequestCount { get; set; }
        }
    }
}