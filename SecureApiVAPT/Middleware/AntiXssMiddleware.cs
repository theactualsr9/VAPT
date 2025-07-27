using System.Text.RegularExpressions;
using System.Net;

namespace SecureApiVAPT.Middleware;

public class AntiXssMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AntiXssMiddleware> _logger;

    public AntiXssMiddleware(RequestDelegate next, ILogger<AntiXssMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check query string for XSS
        if (context.Request.QueryString.HasValue)
        {
            var queryString = context.Request.QueryString.Value;
            if (ContainsXss(queryString))
            {
                _logger.LogWarning("XSS attempt detected in query string: {QueryString}", queryString);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid input detected");
                return;
            }
        }

        // Check request body for XSS (for POST/PUT requests)
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (ContainsXss(body))
            {
                _logger.LogWarning("XSS attempt detected in request body");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid input detected");
                return;
            }
        }

        await _next(context);
    }

    private static bool ContainsXss(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        // Check both original and URL-decoded input
        var decodedInput = System.Net.WebUtility.UrlDecode(input);
        var inputs = new[] { input, decodedInput };

        var xssPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"on\w+\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>",
            @"<link[^>]*>",
            @"<meta[^>]*>",
            @"<img[^>]*onerror[^>]*>",
            @"vbscript:",
            @"data:",
            @"&#x?[0-9a-f]+;",
            @"&#[0-9]+;",
            @"%3C.*?%3E",  // URL encoded < and >
            @"&lt;.*?&gt;", // HTML encoded < and >
        };

        return inputs.Any(inp => xssPatterns.Any(pattern => Regex.IsMatch(inp, pattern, RegexOptions.IgnoreCase)));
    }
} 