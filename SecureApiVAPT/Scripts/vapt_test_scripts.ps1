# VAPT Security Testing Scripts for Secure API
# This script provides various security testing scenarios

param(
    [string]$BaseUrl = "https://localhost:5001",
    [string]$OutputPath = "vapt_results",
    [switch]$RunAll,
    [switch]$TestAuth,
    [switch]$TestInjection,
    [switch]$TestRateLimit,
    [switch]$TestHeaders
)

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = "$OutputPath/vapt_test_$timestamp.log"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $logMessage = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'): [$Level] $Message"
    Write-Host $logMessage
    Add-Content -Path $logFile -Value $logMessage
}

function Test-Authentication {
    Write-Log "Testing Authentication Security..." "INFO"
    
    # Test 1: Valid registration
    $registerData = @{
        username = "vapt_test_user"
        email = "vapt@test.com"
        password = "SecurePass123!"
        age = 25
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/register" -Method POST -Body $registerData -ContentType "application/json"
        Write-Log "✓ Registration successful" "SUCCESS"
        $token = $response.token
    }
    catch {
        Write-Log "✗ Registration failed: $($_.Exception.Message)" "ERROR"
        return
    }
    
    # Test 2: Invalid credentials
    $invalidLogin = @{
        username = "vapt_test_user"
        password = "WrongPassword123!"
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -Body $invalidLogin -ContentType "application/json"
        Write-Log "✗ Invalid login should have failed" "ERROR"
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 401) {
            Write-Log "✓ Invalid credentials properly rejected" "SUCCESS"
        }
        else {
            Write-Log "✗ Unexpected response for invalid credentials: $($_.Exception.Response.StatusCode)" "ERROR"
        }
    }
    
    # Test 3: Weak password
    $weakPassword = @{
        username = "weak_user"
        email = "weak@test.com"
        password = "123"
        age = 25
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/register" -Method POST -Body $weakPassword -ContentType "application/json"
        Write-Log "✗ Weak password should have been rejected" "ERROR"
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 400) {
            Write-Log "✓ Weak password properly rejected" "SUCCESS"
        }
        else {
            Write-Log "✗ Weak password not properly validated: $($_.Exception.Response.StatusCode)" "ERROR"
        }
    }
}

function Test-SQLInjection {
    Write-Log "Testing SQL Injection Protection..." "INFO"
    
    $sqlInjectionPayloads = @(
        "'; DROP TABLE Users; --",
        "' OR '1'='1",
        "' UNION SELECT * FROM Users --",
        "'; EXEC xp_cmdshell('dir'); --",
        "' OR 1=1 --",
        "admin'--"
    )
    
    foreach ($payload in $sqlInjectionPayloads) {
        $encodedPayload = [System.Web.HttpUtility]::UrlEncode($payload)
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/users/search?username=$encodedPayload" -Method GET
            Write-Log "✗ SQL injection not blocked: $payload" "ERROR"
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 400) {
                Write-Log "✓ SQL injection blocked: $payload" "SUCCESS"
            }
            else {
                Write-Log "✗ SQL injection not properly handled: $payload - $($_.Exception.Response.StatusCode)" "ERROR"
            }
        }
    }
}

function Test-XSS {
    Write-Log "Testing XSS Protection..." "INFO"
    
    $xssPayloads = @(
        "<script>alert('XSS')</script>",
        "javascript:alert('XSS')",
        "<img src=x onerror=alert('XSS')>",
        "<iframe src=javascript:alert('XSS')></iframe>",
        "&#x3C;script&#x3E;alert('XSS')&#x3C;/script&#x3E;"
    )
    
    foreach ($payload in $xssPayloads) {
        $encodedPayload = [System.Web.HttpUtility]::UrlEncode($payload)
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/users/search?username=$encodedPayload" -Method GET
            Write-Log "✗ XSS not blocked: $payload" "ERROR"
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 400) {
                Write-Log "✓ XSS blocked: $payload" "SUCCESS"
            }
            else {
                Write-Log "✗ XSS not properly handled: $payload - $($_.Exception.Response.StatusCode)" "ERROR"
            }
        }
    }
}

function Test-RateLimiting {
    Write-Log "Testing Rate Limiting..." "INFO"
    
    $rateLimitedCount = 0
    $totalRequests = 35
    
    for ($i = 1; $i -le $totalRequests; $i++) {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/products" -Method GET
            Write-Log "Request $i: Success" "DEBUG"
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 429) {
                $rateLimitedCount++
                Write-Log "Request $i: Rate limited" "DEBUG"
            }
            else {
                Write-Log "Request $i: Unexpected error - $($_.Exception.Response.StatusCode)" "ERROR"
            }
        }
        
        # Small delay to avoid overwhelming the server
        Start-Sleep -Milliseconds 100
    }
    
    if ($rateLimitedCount -gt 0) {
        Write-Log "✓ Rate limiting working: $rateLimitedCount requests were rate limited" "SUCCESS"
    }
    else {
        Write-Log "✗ Rate limiting not working properly" "ERROR"
    }
}

function Test-SecurityHeaders {
    Write-Log "Testing Security Headers..." "INFO"
    
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/products" -Method GET
        $headers = $response.Headers
        
        $requiredHeaders = @{
            "X-Frame-Options" = "DENY"
            "X-XSS-Protection" = "1; mode=block"
            "X-Content-Type-Options" = "nosniff"
            "Strict-Transport-Security" = "max-age=31536000; includeSubDomains"
            "Referrer-Policy" = "strict-origin-when-cross-origin"
        }
        
        foreach ($header in $requiredHeaders.GetEnumerator()) {
            if ($headers.Contains($header.Key) -and $headers[$header.Key] -eq $header.Value) {
                Write-Log "✓ Security header $($header.Key) present and correct" "SUCCESS"
            }
            else {
                Write-Log "✗ Security header $($header.Key) missing or incorrect" "ERROR"
            }
        }
    }
    catch {
        Write-Log "✗ Failed to test security headers: $($_.Exception.Message)" "ERROR"
    }
}

function Test-CORS {
    Write-Log "Testing CORS Policy..." "INFO"
    
    $testOrigins = @(
        "https://malicious-site.com",
        "http://localhost:3000",
        "https://yourdomain.com"
    )
    
    foreach ($origin in $testOrigins) {
        try {
            $headers = @{
                "Origin" = $origin
            }
            
            $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/products" -Method GET -Headers $headers
            $corsHeader = $response.Headers["Access-Control-Allow-Origin"]
            
            if ($origin -eq "https://yourdomain.com" -and $corsHeader -eq $origin) {
                Write-Log "✓ CORS allows authorized origin: $origin" "SUCCESS"
            }
            elseif ($origin -ne "https://yourdomain.com" -and [string]::IsNullOrEmpty($corsHeader)) {
                Write-Log "✓ CORS properly blocks unauthorized origin: $origin" "SUCCESS"
            }
            else {
                Write-Log "✗ CORS policy issue with origin: $origin" "ERROR"
            }
        }
        catch {
            Write-Log "✗ CORS test failed for origin $origin : $($_.Exception.Message)" "ERROR"
        }
    }
}

function Test-ErrorHandling {
    Write-Log "Testing Error Handling..." "INFO"
    
    # Test non-existent endpoint
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/nonexistent" -Method GET
        Write-Log "✗ Non-existent endpoint should return 404" "ERROR"
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Log "✓ 404 error properly handled" "SUCCESS"
        }
        else {
            Write-Log "✗ 404 error not properly handled: $($_.Exception.Response.StatusCode)" "ERROR"
        }
    }
    
    # Test malformed JSON
    $malformedJson = "{ invalid json }"
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/register" -Method POST -Body $malformedJson -ContentType "application/json"
        Write-Log "✗ Malformed JSON should be rejected" "ERROR"
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 400) {
            Write-Log "✓ Malformed JSON properly rejected" "SUCCESS"
        }
        else {
            Write-Log "✗ Malformed JSON not properly handled: $($_.Exception.Response.StatusCode)" "ERROR"
        }
    }
}

# Main execution
Write-Log "🚀 Starting VAPT Security Testing" "INFO"
Write-Log "Target URL: $BaseUrl" "INFO"
Write-Log "Output Path: $OutputPath" "INFO"
Write-Log "Timestamp: $(Get-Date)" "INFO"

if ($RunAll -or $TestAuth) {
    Test-Authentication
}

if ($RunAll -or $TestInjection) {
    Test-SQLInjection
    Test-XSS
}

if ($RunAll -or $TestRateLimit) {
    Test-RateLimiting
}

if ($RunAll -or $TestHeaders) {
    Test-SecurityHeaders
    Test-CORS
}

if ($RunAll) {
    Test-ErrorHandling
}

Write-Log "✅ VAPT Security Testing Completed" "INFO"
Write-Log "Results saved to: $logFile" "INFO" 