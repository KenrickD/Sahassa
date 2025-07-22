using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using WMS.Application.Exceptions;

namespace WMS.API.Middleware;

public class ExceptionMiddleware
{
    private RequestDelegate Next { get; set; }

    public ExceptionMiddleware(RequestDelegate next)
    {
        Next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (DataNotFoundByIDException ex)
        {
            int statusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails()
            {
                Status = statusCode,
                Detail = string.Empty,
                Instance = "",
                Title = $"{ex.ObjectName} for id {ex.Id} not found",
                Type = "Error"
            };

            var problemDetailJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailJson);
        }
        catch (DataNotFoundException ex)
        {
            int statusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails()
            {
                Status = statusCode,
                Detail = $"{ex.ObjectName} find by {ex.FieldName} with value {ex.FieldValue} not found",
                Instance = "",
                Title = "Data Not Found Error",
                Type = "Error"
            };

            var problemDetailJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailJson);
        }
        catch (ErrorValidationException ex)
        {
            int statusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails()
            {
                Status = statusCode,
                Detail = ex.Message,
                Instance = "",
                Title = "Validation Error",
                Type = "Error"
            };

            var problemDetailJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailJson);
        }
        catch (ValidationException ex)
        {
            int statusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails()
            {
                Status = statusCode,
                Detail = JsonSerializer.Serialize(ex.InnerException),
                Instance = "",
                Title = "Validation Error",
                Type = "Error"
            };

            var problemDetailJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailJson);
        }
        catch (Exception ex)
        {
            int statusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails()
            {
                Status = statusCode,
                Detail = ex.Message,
                Instance = "",
                Title = "Internal Server Error - something went wrong",
                Type = "Error"
            };

            var problemDetailJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailJson);
        }
    }
}
