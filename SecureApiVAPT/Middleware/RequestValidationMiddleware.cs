namespace SecureApiVAPT.Middleware;

public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;
    private const int MaxRequestSize = 10 * 1024 * 1024; // 10MB

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check request size
        if (context.Request.ContentLength > MaxRequestSize)
        {
            _logger.LogWarning("Request too large: {Size} bytes", context.Request.ContentLength);
            context.Response.StatusCode = 413; // Payload Too Large
            await context.Response.WriteAsync("Request too large");
            return;
        }

        // Check for malformed JSON in POST/PUT requests
        if ((context.Request.Method == "POST" || context.Request.Method == "PUT") && 
            context.Request.ContentType?.Contains("application/json") == true)
        {
            try
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrEmpty(body))
                {
                    System.Text.Json.JsonDocument.Parse(body);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                _logger.LogWarning("Malformed JSON detected");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid JSON format");
                return;
            }
        }

        // Check for suspicious headers
        var suspiciousHeaders = new[] { "X-Forwarded-For", "X-Real-IP", "X-Forwarded-Host" };
        foreach (var header in suspiciousHeaders)
        {
            if (context.Request.Headers.ContainsKey(header))
            {
                var value = context.Request.Headers[header].ToString();
                if (ContainsSuspiciousContent(value))
                {
                    _logger.LogWarning("Suspicious content in header {Header}: {Value}", header, value);
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid request");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool ContainsSuspiciousContent(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        var suspiciousPatterns = new[]
        {
            @"<script",
            @"javascript:",
            @"vbscript:",
            @"data:",
            @"file:",
            @"ftp:",
            @"gopher:",
            @"telnet:",
            @"news:",
            @"mailto:",
            @"\b(UNION|SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER)\b",
            @"\b(OR|AND)\s+\d+\s*=\s*\d+",
            @"--",
            @"/\*",
            @"\*/"
        };

        return suspiciousPatterns.Any(pattern => 
            System.Text.RegularExpressions.Regex.IsMatch(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
} 