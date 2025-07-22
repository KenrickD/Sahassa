using System.Net;
using System.Text.Json;
using WMS.Domain.DTOs.Common;

namespace WMS.WebAPI.Middleware
{
    /// <summary>
    /// Global exception handling middleware for API
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault() ??
                                   context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                                   Guid.NewGuid().ToString();

                _logger.LogError(ex, "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                    correlationId, context.Request.Path, context.Request.Method);

                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message, errors) = exception switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access", new[] { "Access denied" }),
                ArgumentNullException nullEx => (HttpStatusCode.BadRequest, "Missing required data", new[] { nullEx.ParamName ?? "Unknown parameter" }),
                ArgumentException argEx => (HttpStatusCode.BadRequest, "Invalid request", new[] { argEx.Message }),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found", new[] { "The requested resource was not found" }),
                InvalidOperationException opEx => (HttpStatusCode.Conflict, "Operation not allowed", new[] { opEx.Message }),
                TimeoutException => (HttpStatusCode.RequestTimeout, "Request timeout", new[] { "The request took too long to complete" }),
                _ => (HttpStatusCode.InternalServerError, "An internal server error occurred", new[] { "Please try again later or contact support" })
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new ApiResponseDto<object>
            {
                Success = false,
                Message = message,
                Errors = errors.ToList(),
                Timestamp = DateTime.UtcNow
            };

            // Add correlation ID to error response
            if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
            {
                context.Response.Headers.Add("X-Correlation-ID", correlationId);
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}