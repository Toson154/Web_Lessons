// إنشاء ملف جديد: Middleware/PerformanceMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Web_Lessons.Middleware
{
    public class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMiddleware> _logger;

        public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Set response headers for performance
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // Cache static files for 1 year
            if (context.Request.Path.Value.Contains("/images/") ||
                context.Request.Path.Value.Contains("/css/") ||
                context.Request.Path.Value.Contains("/js/"))
            {
                context.Response.Headers.Append("Cache-Control", "public, max-age=31536000");
            }
            // Cache dynamic content for 5 minutes
            else if (context.Request.Path.Value.Contains("/api/"))
            {
                context.Response.Headers.Append("Cache-Control", "private, max-age=300");
            }

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log slow requests
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning($"Slow request: {context.Request.Path} took {stopwatch.ElapsedMilliseconds}ms");
                }

                // Add server timing header
                context.Response.Headers.Append("Server-Timing", $"total;dur={stopwatch.ElapsedMilliseconds}");
            }
        }
    }
}