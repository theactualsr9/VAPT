# VAPT Testing Guide for Secure API

This guide provides comprehensive instructions for performing Vulnerability Assessment and Penetration Testing (VAPT) on the Secure API using the provided testing tools and scripts.

## 🎯 Overview

The VAPT testing suite includes multiple tools and approaches to thoroughly test the security measures implemented in the Secure API:

1. **C# Testing Framework** - Automated tests with detailed reporting
2. **PowerShell Scripts** - Windows-based testing automation
3. **Bash Scripts** - Linux/macOS testing with curl
4. **Postman Collection** - Manual testing with automated assertions
5. **Comprehensive Test Coverage** - All security aspects covered

## 🛠️ Prerequisites

### Required Software
- .NET 9.0 SDK
- PowerShell 5.1+ (for Windows scripts)
- Bash shell (for Linux/macOS scripts)
- curl (for command-line testing)
- Postman (for collection-based testing)
- jq (for JSON parsing in bash scripts)

### API Setup
1. Ensure the Secure API is running on `https://localhost:5001`
2. Verify all security middleware is properly configured
3. Check that the database is accessible and configured

## 🚀 Running VAPT Tests

### Method 1: C# Testing Framework (Recommended)

The C# testing framework provides the most comprehensive and automated testing experience.

#### Setup
```bash
cd SecureApiVAPT/Tests
dotnet restore
dotnet build
```

#### Run All Tests
```bash
dotnet run
```

#### Run with Custom Target
```bash
dotnet run https://your-api-url.com
```

#### Expected Output
```
🚀 Starting VAPT Security Testing Suite
Target API: https://localhost:5001
Timestamp: 2024-01-15T10:30:00Z

Testing Authentication...
✓ Registration successful
✓ Login successful
✓ Invalid credentials properly rejected
✓ Weak password properly rejected

Testing Authorization...
✓ Protected endpoint accessible with valid token
✓ Protected endpoint properly requires authentication
✓ Admin endpoint properly restricted for regular users

Testing Input Validation...
✓ Malformed JSON properly rejected
✓ Oversized payload properly rejected
✓ Invalid email format properly rejected

Testing Rate Limiting...
✓ Rate limiting working: 5 requests were rate limited

Testing Security Headers...
✓ Security header X-Frame-Options present and correct
✓ Security header X-XSS-Protection present and correct
✓ Security header X-Content-Type-Options present and correct
✓ Security header Strict-Transport-Security present and correct
✓ Security header Referrer-Policy present and correct

Testing CORS Policy...
✓ CORS allows authorized origin: https://yourdomain.com
✓ CORS properly blocks unauthorized origin: https://malicious-site.com

Testing SQL Injection Protection...
✓ SQL injection blocked: '; DROP TABLE Users; --
✓ SQL injection blocked: ' OR '1'='1
✓ SQL injection blocked: ' UNION SELECT * FROM Users --

Testing XSS Protection...
✓ XSS blocked: <script>alert('XSS')</script>
✓ XSS blocked: javascript:alert('XSS')
✓ XSS blocked: <img src=x onerror=alert('XSS')>

Testing Error Handling...
✓ 404 error properly handled
✓ Invalid user ID properly handled
✓ Error response doesn't contain sensitive information

Testing File Upload Security...
✓ Valid file upload successful
✓ Malicious file upload blocked

Testing JWT Token Security...
✓ Expired token properly rejected
✓ Tampered token properly rejected
✓ Unsigned token properly rejected

Testing Privilege Escalation...
✓ Admin endpoint properly protected: /api/v1/users
✓ User modification properly restricted

Testing Session Management...
✓ Token can be reused (expected for JWT)
✓ Concurrent requests with same token work properly

Testing Logging and Monitoring...
✓ Failed login attempt properly handled (should be logged)
✓ SQL injection attempt blocked (should be logged as security event)
✓ Rate limiting events occurred (should be logged): 5 requests rate limited

✅ All VAPT tests completed successfully
📊 VAPT Testing Summary
Duration: 00:02:15
Completed at: 2024-01-15T10:32:15Z
📄 VAPT Report generated: vapt_report_20240115_103215.json
📈 Summary: 45/45 tests passed
```

### Method 2: PowerShell Scripts (Windows)

#### Run All Tests
```powershell
.\Scripts\vapt_test_scripts.ps1 -BaseUrl "https://localhost:5001" -RunAll
```

#### Run Specific Test Categories
```powershell
# Test authentication only
.\Scripts\vapt_test_scripts.ps1 -TestAuth

# Test injection attacks
.\Scripts\vapt_test_scripts.ps1 -TestInjection

# Test rate limiting
.\Scripts\vapt_test_scripts.ps1 -TestRateLimit

# Test security headers
.\Scripts\vapt_test_scripts.ps1 -TestHeaders
```

#### Custom Output Directory
```powershell
.\Scripts\vapt_test_scripts.ps1 -BaseUrl "https://localhost:5001" -OutputPath "custom_results" -RunAll
```

### Method 3: Bash Scripts (Linux/macOS)

#### Make Script Executable
```bash
chmod +x Scripts/vapt_test_curl.sh
```

#### Run All Tests
```bash
./Scripts/vapt_test_curl.sh https://localhost:5001
```

#### Run with Default URL
```bash
./Scripts/vapt_test_curl.sh
```

### Method 4: Postman Collection

#### Import Collection
1. Open Postman
2. Click "Import" button
3. Select the file: `Scripts/vapt_postman_collection.json`
4. The collection will be imported with all test scenarios

#### Configure Environment Variables
1. Create a new environment in Postman
2. Set the following variables:
   - `baseUrl`: `https://localhost:5001`
   - `userToken`: (will be automatically set during tests)
   - `adminToken`: (will be automatically set during tests)

#### Run Collection
1. Select the imported collection
2. Click "Run collection"
3. Choose the environment you created
4. Click "Run Secure API VAPT Testing"

## 📋 Test Categories

### 1. Authentication Testing
- **Valid Registration**: Tests proper user registration with strong password
- **Weak Password**: Verifies password strength validation
- **Valid Login**: Tests successful authentication
- **Invalid Credentials**: Ensures failed login attempts are properly handled
- **Password Validation**: Checks password complexity requirements

### 2. Authorization Testing
- **Protected Endpoints**: Verifies authentication requirements
- **Role-Based Access**: Tests admin vs user permissions
- **Token Validation**: Ensures JWT tokens are properly validated
- **Privilege Escalation**: Attempts to access admin functions with user tokens

### 3. Input Validation Testing
- **Malformed JSON**: Tests handling of invalid JSON payloads
- **Oversized Payloads**: Verifies request size limits
- **Invalid Email Formats**: Tests email validation
- **Data Type Validation**: Ensures proper data type checking

### 4. SQL Injection Testing
- **Query Parameter Injection**: Tests SQL injection in URL parameters
- **Request Body Injection**: Tests SQL injection in JSON payloads
- **Various Payloads**: Tests multiple SQL injection techniques
- **Pattern Detection**: Verifies middleware blocks malicious patterns

### 5. XSS Testing
- **Script Injection**: Tests `<script>` tag injection
- **Event Handler Injection**: Tests `onerror`, `onload` handlers
- **JavaScript Protocol**: Tests `javascript:` protocol injection
- **HTML Entity Encoding**: Tests encoded XSS payloads

### 6. Rate Limiting Testing
- **Request Limits**: Verifies 30 requests per minute limit
- **Rate Limit Headers**: Checks proper 429 status codes
- **Reset Behavior**: Tests rate limit reset functionality

### 7. Security Headers Testing
- **X-Frame-Options**: Prevents clickjacking attacks
- **X-XSS-Protection**: Enables browser XSS protection
- **X-Content-Type-Options**: Prevents MIME type sniffing
- **Strict-Transport-Security**: Enforces HTTPS
- **Referrer-Policy**: Controls referrer information
- **Permissions-Policy**: Restricts browser features

### 8. CORS Testing
- **Authorized Origins**: Tests allowed domain access
- **Unauthorized Origins**: Verifies blocked domain access
- **Header Validation**: Checks CORS header presence

### 9. Error Handling Testing
- **404 Errors**: Tests non-existent endpoint handling
- **Malformed Requests**: Verifies invalid request handling
- **Sensitive Information**: Ensures no data leakage in errors

### 10. JWT Token Security
- **Token Expiration**: Tests expired token handling
- **Token Tampering**: Verifies signature validation
- **Token Format**: Tests malformed token handling

### 11. File Upload Security
- **Valid Files**: Tests allowed file types
- **Malicious Files**: Verifies blocked file types
- **Size Limits**: Tests file size restrictions

### 12. Session Management
- **Token Reuse**: Tests JWT token behavior
- **Concurrent Requests**: Verifies session handling
- **Token Storage**: Tests secure token management

## 📊 Understanding Test Results

### Success Indicators (✓)
- **200/201 Status Codes**: Successful operations
- **400 Status Codes**: Proper validation of invalid input
- **401 Status Codes**: Proper authentication requirements
- **403 Status Codes**: Proper authorization restrictions
- **404 Status Codes**: Proper error handling
- **429 Status Codes**: Rate limiting working correctly

### Failure Indicators (✗)
- **500 Status Codes**: Server errors (may indicate security issues)
- **Unexpected 200**: Endpoints accessible without proper authentication
- **Missing Headers**: Security headers not present
- **Data Leakage**: Sensitive information in error responses

## 🔍 Manual Testing Scenarios

### Advanced SQL Injection Tests
```bash
# Test various SQL injection techniques
curl -X GET "https://localhost:5001/api/v1/users/search?username=' UNION SELECT password FROM users --"
curl -X GET "https://localhost:5001/api/v1/users/search?username='; WAITFOR DELAY '00:00:05'--"
curl -X GET "https://localhost:5001/api/v1/users/search?username=' OR 1=1--"
```

### Advanced XSS Tests
```bash
# Test various XSS payloads
curl -X GET "https://localhost:5001/api/v1/users/search?username=<svg onload=alert('XSS')>"
curl -X GET "https://localhost:5001/api/v1/users/search?username=javascript:void(alert('XSS'))"
curl -X GET "https://localhost:5001/api/v1/users/search?username=&#x3C;script&#x3E;alert('XSS')&#x3C;/script&#x3E;"
```

### JWT Token Manipulation
```bash
# Test with expired token
curl -H "Authorization: Bearer eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ." \
  https://localhost:5001/api/v1/auth/profile

# Test with tampered token
curl -H "Authorization: Bearer YOUR_TOKEN_HERE_tampered" \
  https://localhost:5001/api/v1/auth/profile
```

## 📈 Continuous Testing

### CI/CD Integration
```yaml
# GitHub Actions example
name: VAPT Security Testing
on: [push, pull_request]

jobs:
  vapt-testing:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
      - name: Run VAPT Tests
        run: |
          cd SecureApiVAPT/Tests
          dotnet run https://localhost:5001
```

### Automated Monitoring
```bash
# Cron job for regular testing
0 2 * * * cd /path/to/SecureApiVAPT && ./Scripts/vapt_test_curl.sh > /var/log/vapt_daily.log 2>&1
```

## 🚨 Security Best Practices

### Before Running Tests
1. **Backup Database**: Ensure you have a backup before testing
2. **Test Environment**: Use a dedicated test environment
3. **Monitor Logs**: Watch application logs during testing
4. **Rate Limiting**: Be aware of rate limiting during testing

### After Running Tests
1. **Review Results**: Carefully analyze all test results
2. **Fix Issues**: Address any security vulnerabilities found
3. **Document Findings**: Keep records of test results
4. **Regular Testing**: Schedule regular VAPT testing

### Reporting
1. **Generate Reports**: Use the automated reporting features
2. **Share Results**: Distribute results to security team
3. **Track Improvements**: Monitor security posture over time
4. **Compliance**: Use results for compliance reporting

## 🔧 Troubleshooting

### Common Issues

#### API Not Running
```bash
# Check if API is running
curl -I https://localhost:5001/api/v1/products

# Start the API if needed
cd SecureApiVAPT
dotnet run
```

#### SSL Certificate Issues
```bash
# For development, you can skip SSL verification
curl -k https://localhost:5001/api/v1/products

# Or use HTTP for local testing
curl http://localhost:5000/api/v1/products
```

#### Database Connection Issues
- Check connection string in `appsettings.json`
- Ensure SQL Server is running
- Verify database exists and is accessible

#### Rate Limiting Interference
- Wait for rate limits to reset (usually 1 minute)
- Use different IP addresses for testing
- Adjust rate limiting configuration for testing

### Getting Help

1. **Check Logs**: Review application logs for errors
2. **Verify Configuration**: Ensure all security middleware is enabled
3. **Test Individually**: Run tests one by one to isolate issues
4. **Update Dependencies**: Ensure all packages are up to date

## 📚 Additional Resources

- [OWASP Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [Microsoft Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [JWT Security Best Practices](https://auth0.com/blog/a-look-at-the-latest-draft-for-jwt-bcp/)

---

**Note**: This VAPT testing suite is designed for educational and security assessment purposes. Always ensure you have proper authorization before testing any system, and follow responsible disclosure practices if vulnerabilities are found. 