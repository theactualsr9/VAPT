using AspNetCoreRateLimit;
using SecureApiVAPT.Middleware;

namespace SecureApiVAPT.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApplicationMiddleware(this IApplicationBuilder app)
    {
        // Global Exception Handling Middleware (should be first)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Security Headers Middleware
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Request Validation Middleware
        app.UseMiddleware<RequestValidationMiddleware>();

        // Anti-XSS Middleware
        app.UseMiddleware<AntiXssMiddleware>();

        // SQL Injection Prevention Middleware
        app.UseMiddleware<SqlInjectionMiddleware>();

        // HTTPS Redirection
        app.UseHttpsRedirection();

        // CORS
        app.UseCors("DefaultPolicy");

        // Rate Limiting
        app.UseIpRateLimiting();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
} 