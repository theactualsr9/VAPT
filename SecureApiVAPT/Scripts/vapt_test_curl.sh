#!/bin/bash

# VAPT Security Testing Script using curl
# This script tests various security aspects of the Secure API

BASE_URL="${1:-https://localhost:5001}"
OUTPUT_DIR="vapt_results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
LOG_FILE="$OUTPUT_DIR/vapt_test_$TIMESTAMP.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Logging function
log() {
    local level=$1
    local message=$2
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo -e "[$timestamp] [$level] $message" | tee -a "$LOG_FILE"
}

# Test authentication
test_authentication() {
    log "INFO" "Testing Authentication Security..."
    
    # Test 1: Valid registration
    log "INFO" "Testing user registration..."
    REGISTER_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$BASE_URL/api/v1/auth/register" \
        -H "Content-Type: application/json" \
        -d '{
            "username": "vapt_test_user",
            "email": "vapt@test.com",
            "password": "SecurePass123!",
            "age": 25
        }')
    
    HTTP_CODE="${REGISTER_RESPONSE: -3}"
    RESPONSE_BODY="${REGISTER_RESPONSE%???}"
    
    if [ "$HTTP_CODE" -eq 201 ]; then
        log "SUCCESS" "✓ Registration successful"
        TOKEN=$(echo "$RESPONSE_BODY" | jq -r '.token')
    else
        log "ERROR" "✗ Registration failed with HTTP $HTTP_CODE"
        return 1
    fi
    
    # Test 2: Invalid credentials
    log "INFO" "Testing invalid credentials..."
    INVALID_LOGIN_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$BASE_URL/api/v1/auth/login" \
        -H "Content-Type: application/json" \
        -d '{
            "username": "vapt_test_user",
            "password": "WrongPassword123!"
        }')
    
    HTTP_CODE="${INVALID_LOGIN_RESPONSE: -3}"
    
    if [ "$HTTP_CODE" -eq 401 ]; then
        log "SUCCESS" "✓ Invalid credentials properly rejected"
    else
        log "ERROR" "✗ Invalid credentials not properly handled: HTTP $HTTP_CODE"
    fi
    
    # Test 3: Weak password
    log "INFO" "Testing weak password validation..."
    WEAK_PASSWORD_RESPONSE=$(curl -s -w "%{http_code}" -X POST "$BASE_URL/api/v1/auth/register" \
        -H "Content-Type: application/json" \
        -d '{
            "username": "weak_user",
            "email": "weak@test.com",
            "password": "123",
            "age": 25
        }')
    
    HTTP_CODE="${WEAK_PASSWORD_RESPONSE: -3}"
    
    if [ "$HTTP_CODE" -eq 400 ]; then
        log "SUCCESS" "✓ Weak password properly rejected"
    else
        log "ERROR" "✗ Weak password not properly validated: HTTP $HTTP_CODE"
    fi
}

# Test SQL injection protection
test_sql_injection() {
    log "INFO" "Testing SQL Injection Protection..."
    
    local sql_payloads=(
        "'; DROP TABLE Users; --"
        "' OR '1'='1"
        "' UNION SELECT * FROM Users --"
        "'; EXEC xp_cmdshell('dir'); --"
        "' OR 1=1 --"
        "admin'--"
    )
    
    for payload in "${sql_payloads[@]}"; do
        local encoded_payload=$(echo "$payload" | sed 's/ /%20/g' | sed 's/"/%22/g' | sed "s/'/%27/g")
        
        local response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/api/v1/users/search?username=$encoded_payload")
        local http_code="${response: -3}"
        
        if [ "$http_code" -eq 400 ]; then
            log "SUCCESS" "✓ SQL injection blocked: $payload"
        else
            log "ERROR" "✗ SQL injection not blocked: $payload (HTTP $http_code)"
        fi
    done
}

# Test XSS protection
test_xss() {
    log "INFO" "Testing XSS Protection..."
    
    local xss_payloads=(
        "<script>alert('XSS')</script>"
        "javascript:alert('XSS')"
        "<img src=x onerror=alert('XSS')>"
        "<iframe src=javascript:alert('XSS')></iframe>"
        "&#x3C;script&#x3E;alert('XSS')&#x3C;/script&#x3E;"
    )
    
    for payload in "${xss_payloads[@]}"; do
        local encoded_payload=$(echo "$payload" | sed 's/ /%20/g' | sed 's/"/%22/g' | sed "s/'/%27/g")
        
        local response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/api/v1/users/search?username=$encoded_payload")
        local http_code="${response: -3}"
        
        if [ "$http_code" -eq 400 ]; then
            log "SUCCESS" "✓ XSS blocked: $payload"
        else
            log "ERROR" "✗ XSS not blocked: $payload (HTTP $http_code)"
        fi
    done
}

# Test rate limiting
test_rate_limiting() {
    log "INFO" "Testing Rate Limiting..."
    
    local rate_limited_count=0
    local total_requests=35
    
    for ((i=1; i<=total_requests; i++)); do
        local response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/api/v1/products")
        local http_code="${response: -3}"
        
        if [ "$http_code" -eq 429 ]; then
            ((rate_limited_count++))
            log "DEBUG" "Request $i: Rate limited"
        elif [ "$http_code" -eq 200 ]; then
            log "DEBUG" "Request $i: Success"
        else
            log "ERROR" "Request $i: Unexpected error - HTTP $http_code"
        fi
        
        # Small delay to avoid overwhelming the server
        sleep 0.1
    done
    
    if [ "$rate_limited_count" -gt 0 ]; then
        log "SUCCESS" "✓ Rate limiting working: $rate_limited_count requests were rate limited"
    else
        log "ERROR" "✗ Rate limiting not working properly"
    fi
}

# Test security headers
test_security_headers() {
    log "INFO" "Testing Security Headers..."
    
    local response=$(curl -s -I "$BASE_URL/api/v1/products")
    
    local required_headers=(
        "X-Frame-Options: DENY"
        "X-XSS-Protection: 1; mode=block"
        "X-Content-Type-Options: nosniff"
        "Strict-Transport-Security: max-age=31536000; includeSubDomains"
        "Referrer-Policy: strict-origin-when-cross-origin"
    )
    
    for header in "${required_headers[@]}"; do
        local header_name=$(echo "$header" | cut -d: -f1)
        local expected_value=$(echo "$header" | cut -d: -f2- | sed 's/^ //')
        
        if echo "$response" | grep -q "$header_name: $expected_value"; then
            log "SUCCESS" "✓ Security header $header_name present and correct"
        else
            log "ERROR" "✗ Security header $header_name missing or incorrect"
        fi
    done
}

# Test CORS policy
test_cors() {
    log "INFO" "Testing CORS Policy..."
    
    local test_origins=(
        "https://malicious-site.com"
        "http://localhost:3000"
        "https://yourdomain.com"
    )
    
    for origin in "${test_origins[@]}"; do
        local response=$(curl -s -I -H "Origin: $origin" "$BASE_URL/api/v1/products")
        local cors_header=$(echo "$response" | grep -i "Access-Control-Allow-Origin" | cut -d: -f2- | sed 's/^ //')
        
        if [ "$origin" = "https://yourdomain.com" ] && [ "$cors_header" = "$origin" ]; then
            log "SUCCESS" "✓ CORS allows authorized origin: $origin"
        elif [ "$origin" != "https://yourdomain.com" ] && [ -z "$cors_header" ]; then
            log "SUCCESS" "✓ CORS properly blocks unauthorized origin: $origin"
        else
            log "ERROR" "✗ CORS policy issue with origin: $origin"
        fi
    done
}

# Test error handling
test_error_handling() {
    log "INFO" "Testing Error Handling..."
    
    # Test non-existent endpoint
    local response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/api/v1/nonexistent")
    local http_code="${response: -3}"
    
    if [ "$http_code" -eq 404 ]; then
        log "SUCCESS" "✓ 404 error properly handled"
    else
        log "ERROR" "✗ 404 error not properly handled: HTTP $http_code"
    fi
    
    # Test malformed JSON
    local response=$(curl -s -w "%{http_code}" -X POST "$BASE_URL/api/v1/auth/register" \
        -H "Content-Type: application/json" \
        -d "{ invalid json }")
    local http_code="${response: -3}"
    
    if [ "$http_code" -eq 400 ]; then
        log "SUCCESS" "✓ Malformed JSON properly rejected"
    else
        log "ERROR" "✗ Malformed JSON not properly handled: HTTP $http_code"
    fi
}

# Test authorization
test_authorization() {
    log "INFO" "Testing Authorization..."
    
    if [ -z "$TOKEN" ]; then
        log "WARNING" "Skipping authorization tests - no token available"
        return
    fi
    
    # Test protected endpoint with valid token
    local response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/api/v1/auth/profile" \
        -H "Authorization: Bearer $TOKEN")
    local http_code="${response: -3}"
    
    if [ "$http_code" -eq 200 ]; then
        log "SUCCESS" "✓ Protected endpoint accessible with valid token"
    else
        log "ERROR" "✗ Protected endpoint not accessible with valid token: HTTP $http_code"
    fi
    
    # Test protected endpoint without token
    local response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/api/v1/auth/profile")
    local http_code="${response: -3}"
    
    if [ "$http_code" -eq 401 ]; then
        log "SUCCESS" "✓ Protected endpoint properly requires authentication"
    else
        log "ERROR" "✗ Protected endpoint accessible without authentication: HTTP $http_code"
    fi
    
    # Test admin endpoint with user token
    local response=$(curl -s -w "%{http_code}" -X GET "$BASE_URL/api/v1/users" \
        -H "Authorization: Bearer $TOKEN")
    local http_code="${response: -3}"
    
    if [ "$http_code" -eq 403 ]; then
        log "SUCCESS" "✓ Admin endpoint properly restricted for regular users"
    else
        log "ERROR" "✗ Admin endpoint accessible to regular users: HTTP $http_code"
    fi
}

# Main execution
main() {
    log "INFO" "🚀 Starting VAPT Security Testing"
    log "INFO" "Target URL: $BASE_URL"
    log "INFO" "Output Directory: $OUTPUT_DIR"
    log "INFO" "Timestamp: $(date)"
    
    # Check if jq is available for JSON parsing
    if ! command -v jq &> /dev/null; then
        log "WARNING" "jq is not installed. Some tests may not work properly."
        log "INFO" "Install jq: sudo apt-get install jq (Ubuntu/Debian) or brew install jq (macOS)"
    fi
    
    # Run all tests
    test_authentication
    test_authorization
    test_sql_injection
    test_xss
    test_rate_limiting
    test_security_headers
    test_cors
    test_error_handling
    
    log "INFO" "✅ VAPT Security Testing Completed"
    log "INFO" "Results saved to: $LOG_FILE"
}

# Run main function
main "$@" 