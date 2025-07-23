namespace WMS.WebAPI.Middleware
{
    public class RequestCaptureMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestCaptureMiddleware> _logger;

        public RequestCaptureMiddleware(RequestDelegate next, ILogger<RequestCaptureMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only capture for our problem endpoint
            if (context.Request.Path.StartsWithSegments("/GIV_FinishedGoods/Create"))
            {
                await LogRawRequestAsync(context);
            }

            await _next(context);

            // Log response for failed requests
            if (context.Response.StatusCode >= 400 &&
                context.Request.Path.StartsWithSegments("/GIV_FinishedGoods/Create"))
            {
                _logger.LogError("📤 RESPONSE STATUS: {StatusCode}", context.Response.StatusCode);
                _logger.LogError("📤 RESPONSE HEADERS: {@Headers}",
                    context.Response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));
            }
        }

        private async Task LogRawRequestAsync(HttpContext context)
        {
            try
            {
                _logger.LogError("🔥 === RAW POSTMAN REQUEST CAPTURE ===");
                _logger.LogError("🔥 METHOD: {Method}", context.Request.Method);
                _logger.LogError("🔥 PATH: {Path}", context.Request.Path);
                _logger.LogError("🔥 QUERY STRING: {QueryString}", context.Request.QueryString);
                _logger.LogError("🔥 CONTENT-TYPE: {ContentType}", context.Request.ContentType);
                _logger.LogError("🔥 CONTENT-LENGTH: {ContentLength}", context.Request.ContentLength);

                // Log ALL headers
                _logger.LogError("🔥 HEADERS:");
                foreach (var header in context.Request.Headers)
                {
                    _logger.LogError("🔥   {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                }

                // Enable buffering to read body multiple times
                context.Request.EnableBuffering();

                // Log request body
                if (context.Request.ContentLength > 0)
                {
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0; // Reset for next middleware

                    _logger.LogError("🔥 RAW BODY (first 2000 chars): {Body}",
                        requestBody.Length > 2000 ? requestBody.Substring(0, 2000) + "..." : requestBody);
                }

                // Log form data if it exists
                if (context.Request.HasFormContentType)
                {
                    _logger.LogError("🔥 FORM DATA:");

                    // Log form fields
                    foreach (var field in context.Request.Form)
                    {
                        _logger.LogError("🔥   FIELD [{FieldName}]: {FieldValue}",
                            field.Key, string.Join(", ", field.Value));
                    }

                    // Log form files
                    _logger.LogError("🔥 FORM FILES COUNT: {FileCount}", context.Request.Form.Files.Count);
                    foreach (var file in context.Request.Form.Files)
                    {
                        _logger.LogError("🔥   FILE: Name={Name}, FileName={FileName}, Size={Size}, ContentType={ContentType}",
                            file.Name, file.FileName, file.Length, file.ContentType);
                    }
                }

                _logger.LogError("🔥 === END RAW REQUEST CAPTURE ===");
            }
            catch (Exception ex)
            {
                _logger.LogError("💥 ERROR CAPTURING REQUEST: {Error}", ex.Message);
            }
        }
    }
}
