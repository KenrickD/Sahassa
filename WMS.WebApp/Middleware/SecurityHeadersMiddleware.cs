namespace WMS.WebApp.Middlewares
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Remove server header
            context.Response.Headers.Remove("Server");

            // Add security headers
            var headers = context.Response.Headers;

            // Prevent clickjacking attacks
            headers.Add("X-Frame-Options", "DENY");

            // Prevent MIME type sniffing
            headers.Add("X-Content-Type-Options", "nosniff");

            // Enable XSS protection
            headers.Add("X-XSS-Protection", "1; mode=block");

            // Referrer policy
            headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content Security Policy
            // With this more permissive version:
            var csp = "default-src 'self' 'unsafe-inline' 'unsafe-eval' data: blob:; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
                     "https://cdn.jsdelivr.net " +
                     "https://cdnjs.cloudflare.com " +
                     "https://unpkg.com " +
                     "https://code.jquery.com " +
                     "https://ajax.googleapis.com " +
                     "https://stackpath.bootstrapcdn.com; " +
                     "style-src 'self' 'unsafe-inline' " +
                     "https://cdn.jsdelivr.net " +
                     "https://cdnjs.cloudflare.com " +
                     "https://fonts.googleapis.com " +
                     "https://unpkg.com " +
                     "https://stackpath.bootstrapcdn.com; " +
                     "img-src 'self' data: blob: https: http:; " +
                     "font-src 'self' data: " +
                     "https://cdn.jsdelivr.net " +
                     "https://cdnjs.cloudflare.com " +
                     "https://fonts.gstatic.com " +
                     "https://unpkg.com; " +
                     "connect-src 'self' https: http: ws: wss:; " +
                     "media-src 'self' data: blob: https: http:; " +
                     "object-src 'none'; " +
                     "frame-ancestors 'none'; " +
                     "base-uri 'self'; " +
                     "form-action 'self';";

            headers.Add("Content-Security-Policy", csp);

            // Permissions policy (formerly Feature Policy)
            headers.Add("Permissions-Policy",
                "accelerometer=(), " +
                "camera=(), " +
                "geolocation=(), " +
                "gyroscope=(), " +
                "magnetometer=(), " +
                "microphone=(), " +
                "payment=(), " +
                "usb=()");

            await _next(context);
        }
    }
}