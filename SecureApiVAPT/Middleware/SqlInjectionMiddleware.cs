using System.Text.RegularExpressions;
using System.Net;

namespace SecureApiVAPT.Middleware;

public class SqlInjectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SqlInjectionMiddleware> _logger;

    public SqlInjectionMiddleware(RequestDelegate next, ILogger<SqlInjectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check query string for SQL injection
        if (context.Request.QueryString.HasValue)
        {
            var queryString = context.Request.QueryString.Value;
            if (ContainsSqlInjection(queryString))
            {
                _logger.LogWarning("SQL injection attempt detected in query string: {QueryString}", queryString);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid input detected");
                return;
            }
        }

        // Check request body for SQL injection
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (ContainsSqlInjection(body))
            {
                _logger.LogWarning("SQL injection attempt detected in request body");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid input detected");
                return;
            }
        }

        await _next(context);
    }

    private static bool ContainsSqlInjection(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        // Check both original and URL-decoded input
        var decodedInput = System.Net.WebUtility.UrlDecode(input);
        var inputs = new[] { input, decodedInput };

        var sqlPatterns = new[]
        {
            @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|OR|AND)\b)",
            @"(--|#|/\*|\*/)",
            @"(\b(WAITFOR|DELAY|SLEEP)\b)",
            @"(\b(INFORMATION_SCHEMA|sys\.|sysobjects|syscolumns)\b)",
            @"(\b(xp_|sp_)\w*\b)",
            @"(\b(CAST|CONVERT)\s*\()",
            @"(\b(CHAR|ASCII|SUBSTRING|LEN)\s*\()",
            @"(\b(HAVING|GROUP BY|ORDER BY)\b)",
            @"(\b(UNION ALL|UNION SELECT)\b)",
            @"(\b(OR 1=1|OR '1'='1'|OR ""1""=""1"")\b)",
            @"(\b(AND 1=1|AND '1'='1'|AND ""1""=""1"")\b)",
            @"('.*?OR.*?'.*?=.*?')",
            @"('.*?AND.*?'.*?=.*?')",
            @"(\bOR\s+\d+\s*=\s*\d+\b)",
            @"(\bAND\s+\d+\s*=\s*\d+\b)",
            @"('.*?UNION.*?SELECT)",
            @"('.*?DROP.*?TABLE)",
            @"('.*?INSERT.*?INTO)",
            @"('.*?UPDATE.*?SET)",
            @"('.*?DELETE.*?FROM)"
        };

        return inputs.Any(inp => sqlPatterns.Any(pattern => Regex.IsMatch(inp, pattern, RegexOptions.IgnoreCase)));
    }
} 