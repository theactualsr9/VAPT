using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace SecureApiVAPT.Tests;

public class VAPTTests
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<VAPTTests> _logger;
    private string? _userToken;

    public VAPTTests(string baseUrl, ILogger<VAPTTests> logger)
    {
        _baseUrl = baseUrl;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task RunAllTests()
    {
        _logger.LogInformation("Starting comprehensive VAPT testing...");

        await TestAuthentication();
        await TestAuthorization();
        await TestInputValidation();
        await TestRateLimiting();
        await TestSecurityHeaders();
        await TestCORS();
        await TestSQLInjection();
        await TestXSS();
        await TestErrorHandling();
        await TestFileUpload();
        await TestJWTTokenSecurity();
        await TestPrivilegeEscalation();
        await TestSessionManagement();
        await TestLoggingAndMonitoring();

        _logger.LogInformation("VAPT testing completed!");
    }

    private async Task TestAuthentication()
    {
        _logger.LogInformation("Testing Authentication...");

        // Test 1: Valid registration
        var registerData = new
        {
            username = "testuser_vapt",
            email = "testuser@vapt.com",
            password = "SecurePass123!",
            age = 25
        };

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/register", 
            new StringContent(JsonSerializer.Serialize(registerData), Encoding.UTF8, "application/json"));
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            _userToken = result.GetProperty("token").GetString();
            _logger.LogInformation("✓ Registration successful");
        }
        else
        {
            _logger.LogWarning("✗ Registration failed: {StatusCode}", response.StatusCode);
        }

        // Test 2: Valid login
        var loginData = new { username = "testuser_vapt", password = "SecurePass123!" };
        response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            _userToken = result.GetProperty("token").GetString();
            _logger.LogInformation("✓ Login successful");
        }
        else
        {
            _logger.LogWarning("✗ Login failed: {StatusCode}", response.StatusCode);
        }

        // Test 3: Invalid credentials
        var invalidLoginData = new { username = "testuser_vapt", password = "WrongPassword123!" };
        response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(invalidLoginData), Encoding.UTF8, "application/json"));

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("✓ Invalid credentials properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Invalid credentials not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 4: Weak password
        var weakPasswordData = new
        {
            username = "weakuser",
            email = "weak@test.com",
            password = "123",
            age = 25
        };

        response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/register",
            new StringContent(JsonSerializer.Serialize(weakPasswordData), Encoding.UTF8, "application/json"));

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogInformation("✓ Weak password properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Weak password not properly validated: {StatusCode}", response.StatusCode);
        }
    }

    private async Task TestAuthorization()
    {
        _logger.LogInformation("Testing Authorization...");

        if (string.IsNullOrEmpty(_userToken))
        {
            _logger.LogWarning("Skipping authorization tests - no user token available");
            return;
        }

        // Test 1: Access protected endpoint with valid token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("✓ Protected endpoint accessible with valid token");
        }
        else
        {
            _logger.LogWarning("✗ Protected endpoint not accessible with valid token: {StatusCode}", response.StatusCode);
        }

        // Test 2: Access protected endpoint without token
        _httpClient.DefaultRequestHeaders.Authorization = null;
        response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("✓ Protected endpoint properly requires authentication");
        }
        else
        {
            _logger.LogWarning("✗ Protected endpoint accessible without authentication: {StatusCode}", response.StatusCode);
        }

        // Test 3: Access admin endpoint with user token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);
        response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/users");

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogInformation("✓ Admin endpoint properly restricted for regular users");
        }
        else
        {
            _logger.LogWarning("✗ Admin endpoint accessible to regular users: {StatusCode}", response.StatusCode);
        }

        // Test 4: Access with invalid token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token_here");
        response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("✓ Invalid token properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Invalid token not properly handled: {StatusCode}", response.StatusCode);
        }
    }

    private async Task TestInputValidation()
    {
        _logger.LogInformation("Testing Input Validation...");

        // Test 1: Malformed JSON
        var malformedJson = "{ invalid json }";
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/register",
            new StringContent(malformedJson, Encoding.UTF8, "application/json"));

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogInformation("✓ Malformed JSON properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Malformed JSON not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 2: Oversized payload
        var largePayload = new string('a', 1024 * 1024); // 1MB string
        var oversizedData = new { data = largePayload };
        response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/register",
            new StringContent(JsonSerializer.Serialize(oversizedData), Encoding.UTF8, "application/json"));

        if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
        {
            _logger.LogInformation("✓ Oversized payload properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Oversized payload not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 3: Invalid email format
        var invalidEmailData = new
        {
            username = "testuser",
            email = "invalid-email",
            password = "SecurePass123!",
            age = 25
        };

        response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/register",
            new StringContent(JsonSerializer.Serialize(invalidEmailData), Encoding.UTF8, "application/json"));

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogInformation("✓ Invalid email format properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Invalid email format not properly validated: {StatusCode}", response.StatusCode);
        }
    }

    private async Task TestRateLimiting()
    {
        _logger.LogInformation("Testing Rate Limiting...");

        var requests = new List<Task<HttpResponseMessage>>();
        var endpoint = $"{_baseUrl}/api/v1/products";

        // Send 35 requests (should exceed the 30 per minute limit)
        for (int i = 0; i < 35; i++)
        {
            requests.Add(_httpClient.GetAsync(endpoint));
        }

        var responses = await Task.WhenAll(requests);
        var rateLimitedCount = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);

        if (rateLimitedCount > 0)
        {
            _logger.LogInformation("✓ Rate limiting working: {RateLimitedCount} requests were rate limited", rateLimitedCount);
        }
        else
        {
            _logger.LogWarning("✗ Rate limiting not working properly");
        }

        // Wait for rate limit to reset
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    private async Task TestSecurityHeaders()
    {
        _logger.LogInformation("Testing Security Headers...");

        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/products");
        var headers = response.Headers;

        var requiredHeaders = new Dictionary<string, string>
        {
            { "X-Frame-Options", "DENY" },
            { "X-XSS-Protection", "1; mode=block" },
            { "X-Content-Type-Options", "nosniff" },
            { "Strict-Transport-Security", "max-age=31536000; includeSubDomains" },
            { "Referrer-Policy", "strict-origin-when-cross-origin" },
            { "Permissions-Policy", "geolocation=(), microphone=(), camera=()" }
        };

        foreach (var header in requiredHeaders)
        {
            if (headers.Contains(header.Key) && headers.GetValues(header.Key).FirstOrDefault() == header.Value)
            {
                _logger.LogInformation("✓ Security header {Header} present and correct", header.Key);
            }
            else
            {
                _logger.LogWarning("✗ Security header {Header} missing or incorrect", header.Key);
            }
        }
    }

    private async Task TestCORS()
    {
        _logger.LogInformation("Testing CORS Policy...");

        // Test with different origins
        var testOrigins = new[]
        {
            "https://malicious-site.com",
            "http://localhost:3000",
            "https://yourdomain.com"
        };

        foreach (var origin in testOrigins)
        {
            _httpClient.DefaultRequestHeaders.Remove("Origin");
            _httpClient.DefaultRequestHeaders.Add("Origin", origin);

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/products");
            var corsHeader = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();

            if (origin == "https://yourdomain.com" && corsHeader == origin)
            {
                _logger.LogInformation("✓ CORS allows authorized origin: {Origin}", origin);
            }
            else if (origin != "https://yourdomain.com" && string.IsNullOrEmpty(corsHeader))
            {
                _logger.LogInformation("✓ CORS properly blocks unauthorized origin: {Origin}", origin);
            }
            else
            {
                _logger.LogWarning("✗ CORS policy issue with origin: {Origin}", origin);
            }
        }
    }

    private async Task TestSQLInjection()
    {
        _logger.LogInformation("Testing SQL Injection Protection...");

        var sqlInjectionPayloads = new[]
        {
            "'; DROP TABLE Users; --",
            "' OR '1'='1",
            "' UNION SELECT * FROM Users --",
            "'; EXEC xp_cmdshell('dir'); --",
            "' OR 1=1 --",
            "admin'--",
            "'; WAITFOR DELAY '00:00:05'--"
        };

        foreach (var payload in sqlInjectionPayloads)
        {
            // Test in query parameters
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/users/search?username={Uri.EscapeDataString(payload)}");

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("✓ SQL injection attempt blocked in query parameter: {Payload}", payload);
            }
            else
            {
                _logger.LogWarning("✗ SQL injection attempt not blocked in query parameter: {Payload} - {StatusCode}", payload, response.StatusCode);
            }

            // Test in request body
            var bodyData = new { username = payload, email = "test@test.com", password = "SecurePass123!", age = 25 };
            response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/register",
                new StringContent(JsonSerializer.Serialize(bodyData), Encoding.UTF8, "application/json"));

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("✓ SQL injection attempt blocked in request body: {Payload}", payload);
            }
            else
            {
                _logger.LogWarning("✗ SQL injection attempt not blocked in request body: {Payload} - {StatusCode}", payload, response.StatusCode);
            }
        }
    }

    private async Task TestXSS()
    {
        _logger.LogInformation("Testing XSS Protection...");

        var xssPayloads = new[]
        {
            "<script>alert('XSS')</script>",
            "javascript:alert('XSS')",
            "<img src=x onerror=alert('XSS')>",
            "<iframe src=javascript:alert('XSS')></iframe>",
            "&#x3C;script&#x3E;alert('XSS')&#x3C;/script&#x3E;",
            "<svg onload=alert('XSS')>",
            "javascript:void(alert('XSS'))"
        };

        foreach (var payload in xssPayloads)
        {
            // Test in query parameters
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/users/search?username={Uri.EscapeDataString(payload)}");

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("✓ XSS attempt blocked in query parameter: {Payload}", payload);
            }
            else
            {
                _logger.LogWarning("✗ XSS attempt not blocked in query parameter: {Payload} - {StatusCode}", payload, response.StatusCode);
            }

            // Test in request body
            var bodyData = new { username = payload, email = "test@test.com", password = "SecurePass123!", age = 25 };
            response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/register",
                new StringContent(JsonSerializer.Serialize(bodyData), Encoding.UTF8, "application/json"));

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("✓ XSS attempt blocked in request body: {Payload}", payload);
            }
            else
            {
                _logger.LogWarning("✗ XSS attempt not blocked in request body: {Payload} - {StatusCode}", payload, response.StatusCode);
            }
        }
    }

    private async Task TestErrorHandling()
    {
        _logger.LogInformation("Testing Error Handling...");

        // Test 1: Non-existent endpoint
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/nonexistent");
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("✓ 404 error properly handled");
        }
        else
        {
            _logger.LogWarning("✗ 404 error not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 2: Invalid user ID
        response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/users/999999");
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("✓ Invalid user ID properly handled");
        }
        else
        {
            _logger.LogWarning("✗ Invalid user ID not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 3: Check for sensitive information in error responses
        var content = await response.Content.ReadAsStringAsync();
        var sensitivePatterns = new[] { "password", "connection", "database", "server", "admin" };
        
        var hasSensitiveInfo = sensitivePatterns.Any(pattern => 
            content.ToLower().Contains(pattern));

        if (!hasSensitiveInfo)
        {
            _logger.LogInformation("✓ Error response doesn't contain sensitive information");
        }
        else
        {
            _logger.LogWarning("✗ Error response contains sensitive information");
        }
    }

    private async Task TestFileUpload()
    {
        _logger.LogInformation("Testing File Upload Security...");

        if (string.IsNullOrEmpty(_userToken))
        {
            _logger.LogWarning("Skipping file upload tests - no user token available");
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

        // Test 1: Valid file upload
        using var formData = new MultipartFormDataContent();
        var fileContent = new StringContent("test file content");
        formData.Add(fileContent, "file", "test.txt");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/files/upload", formData);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("✓ Valid file upload successful");
        }
        else
        {
            _logger.LogWarning("✗ Valid file upload failed: {StatusCode}", response.StatusCode);
        }

        // Test 2: Malicious file upload
        using var maliciousFormData = new MultipartFormDataContent();
        var maliciousContent = new StringContent("<script>alert('XSS')</script>");
        maliciousFormData.Add(maliciousContent, "file", "malicious.html");

        response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/files/upload", maliciousFormData);

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogInformation("✓ Malicious file upload blocked");
        }
        else
        {
            _logger.LogWarning("✗ Malicious file upload not blocked: {StatusCode}", response.StatusCode);
        }
    }

    private async Task TestJWTTokenSecurity()
    {
        _logger.LogInformation("Testing JWT Token Security...");

        if (string.IsNullOrEmpty(_userToken))
        {
            _logger.LogWarning("Skipping JWT tests - no token available");
            return;
        }

        // Test 1: Expired token (if we can create one)
        var expiredToken = CreateExpiredToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("✓ Expired token properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Expired token not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 2: Tampered token
        var tamperedToken = _userToken + "tampered";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);
        
        response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("✓ Tampered token properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Tampered token not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 3: Token without signature
        var unsignedToken = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", unsignedToken);
        
        response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("✓ Unsigned token properly rejected");
        }
        else
        {
            _logger.LogWarning("✗ Unsigned token not properly handled: {StatusCode}", response.StatusCode);
        }
    }

    private async Task TestPrivilegeEscalation()
    {
        _logger.LogInformation("Testing Privilege Escalation...");

        if (string.IsNullOrEmpty(_userToken))
        {
            _logger.LogWarning("Skipping privilege escalation tests - no user token available");
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

        // Test 1: Try to access admin endpoints
        var adminEndpoints = new[]
        {
            "/api/v1/users",
            "/api/v1/users/1",
            "/api/v1/products",
            "/api/v1/products/1"
        };

        foreach (var endpoint in adminEndpoints)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogInformation("✓ Admin endpoint properly protected: {Endpoint}", endpoint);
            }
            else
            {
                _logger.LogWarning("✗ Admin endpoint accessible to regular user: {Endpoint} - {StatusCode}", endpoint, response.StatusCode);
            }
        }

        // Test 2: Try to modify other user's data
        var otherUserData = new { username = "otheruser", email = "other@test.com", age = 30 };
        var userModificationResponse = await _httpClient.PutAsync($"{_baseUrl}/api/v1/users/999",
            new StringContent(JsonSerializer.Serialize(otherUserData), Encoding.UTF8, "application/json"));

        if (userModificationResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogInformation("✓ User modification properly restricted");
        }
        else
        {
            _logger.LogWarning("✗ User modification not properly restricted: {StatusCode}", userModificationResponse.StatusCode);
        }
    }

    private async Task TestSessionManagement()
    {
        _logger.LogInformation("Testing Session Management...");

        if (string.IsNullOrEmpty(_userToken))
        {
            _logger.LogWarning("Skipping session management tests - no token available");
            return;
        }

        // Test 1: Token reuse
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);
        
        var response1 = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");
        var response2 = await _httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile");

        if (response1.IsSuccessStatusCode && response2.IsSuccessStatusCode)
        {
            _logger.LogInformation("✓ Token can be reused (expected for JWT)");
        }
        else
        {
            _logger.LogWarning("✗ Token reuse issue: {StatusCode1}, {StatusCode2}", response1.StatusCode, response2.StatusCode);
        }

        // Test 2: Concurrent requests with same token
        var concurrentRequests = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            concurrentRequests.Add(_httpClient.GetAsync($"{_baseUrl}/api/v1/auth/profile"));
        }

        var responses = await Task.WhenAll(concurrentRequests);
        var successCount = responses.Count(r => r.IsSuccessStatusCode);

        if (successCount == 5)
        {
            _logger.LogInformation("✓ Concurrent requests with same token work properly");
        }
        else
        {
            _logger.LogWarning("✗ Concurrent requests with same token have issues: {SuccessCount}/5", successCount);
        }
    }

    private async Task TestLoggingAndMonitoring()
    {
        _logger.LogInformation("Testing Logging and Monitoring...");

        // Test 1: Failed login attempt logging
        var failedLoginData = new { username = "nonexistentuser", password = "wrongpassword" };
        var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(failedLoginData), Encoding.UTF8, "application/json"));

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("✓ Failed login attempt properly handled (should be logged)");
        }
        else
        {
            _logger.LogWarning("✗ Failed login attempt not properly handled: {StatusCode}", response.StatusCode);
        }

        // Test 2: Security event logging
        var sqlInjectionResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/users/search?username='; DROP TABLE Users; --");
        
        if (sqlInjectionResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogInformation("✓ SQL injection attempt blocked (should be logged as security event)");
        }
        else
        {
            _logger.LogWarning("✗ SQL injection attempt not properly handled: {StatusCode}", sqlInjectionResponse.StatusCode);
        }

        // Test 3: Rate limiting event logging
        var rateLimitResponses = new List<HttpResponseMessage>();
        for (int i = 0; i < 35; i++)
        {
            rateLimitResponses.Add(await _httpClient.GetAsync($"{_baseUrl}/api/v1/products"));
        }

        var rateLimitedCount = rateLimitResponses.Count(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
        
        if (rateLimitedCount > 0)
        {
            _logger.LogInformation("✓ Rate limiting events occurred (should be logged): {Count} requests rate limited", rateLimitedCount);
        }
        else
        {
            _logger.LogWarning("✗ No rate limiting events detected");
        }
    }

    private string CreateExpiredToken()
    {
        // This is a simplified example - in practice, you'd need the actual JWT signing key
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"123\",\"exp\":0,\"iat\":0}"));
        return $"{header}.{payload}.";
    }
} 