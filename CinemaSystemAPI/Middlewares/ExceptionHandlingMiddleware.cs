using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystemAPI.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "Lỗi không xác định khi xử lý request: {Method} {Path}", context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Thiết lập chuẩn Content-Type theo RFC 7807
            context.Response.ContentType = "application/problem+json";

            var statusCode = exception switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = statusCode;

            // Cấu trúc chuẩn RFC 7807 ProblemDetails
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    404 => "Not Found",
                    _ => "Internal Server Error"
                },
                Detail = exception.Message,
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.com/{statusCode}"
            };

            // Mở rộng thông tin truy vết lỗi (Structured Trace)
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
