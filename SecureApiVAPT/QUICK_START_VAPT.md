# 🚀 Quick Start VAPT Testing Guide

## ⚡ Fast Setup (Choose Your Method)

### Method 1: C# Framework (Most Comprehensive)
```bash
cd SecureApiVAPT/Tests
dotnet run
```

### Method 2: Windows (PowerShell)
```bash
# Double-click or run:
SecureApiVAPT/Scripts/run_vapt_tests.bat

# Or manually:
powershell -ExecutionPolicy Bypass -File "Scripts/vapt_test_scripts.ps1" -RunAll
```

### Method 3: Linux/macOS (Bash)
```bash
chmod +x SecureApiVAPT/Scripts/vapt_test_curl.sh
./SecureApiVAPT/Scripts/vapt_test_curl.sh
```

### Method 4: Postman Collection
1. Import `Scripts/vapt_postman_collection.json` into Postman
2. Set `baseUrl` variable to `https://localhost:5001`
3. Run the collection

## 🎯 What Gets Tested

✅ **Authentication** - Login, registration, password strength  
✅ **Authorization** - Role-based access, JWT tokens  
✅ **SQL Injection** - Multiple attack vectors blocked  
✅ **XSS Protection** - Script injection attempts blocked  
✅ **Rate Limiting** - 30 requests/minute limit enforced  
✅ **Security Headers** - All OWASP recommended headers  
✅ **CORS Policy** - Origin validation working  
✅ **Error Handling** - No sensitive data leakage  
✅ **Input Validation** - Malformed requests rejected  
✅ **File Upload Security** - Malicious files blocked  

## 📊 Expected Results

When tests pass, you'll see:
```
✅ All VAPT tests completed successfully
📈 Summary: 45/45 tests passed
📄 VAPT Report generated: vapt_report_YYYYMMDD_HHMMSS.json
```

## 🔧 Prerequisites

1. **API Running**: Ensure your Secure API is running on `https://localhost:5001`
2. **Database**: SQL Server should be accessible
3. **Dependencies**: .NET 9.0 SDK (for C# tests)

## 🚨 Common Issues

### API Not Running
```bash
cd SecureApiVAPT
dotnet run
```

### SSL Certificate Issues
```bash
# For development, use HTTP instead
dotnet run --urls "http://localhost:5000"
```

### Rate Limiting
- Wait 1 minute between test runs
- Tests will automatically handle rate limiting

## 📁 Output Files

- **Logs**: `vapt_results/vapt_test_YYYYMMDD_HHMMSS.log`
- **Reports**: `vapt_report_YYYYMMDD_HHMMSS.json`
- **C# Logs**: `logs/vapt_tests.log`

## 🎯 Quick Test Commands

### Test Specific Categories
```bash
# Authentication only
powershell -File "Scripts/vapt_test_scripts.ps1" -TestAuth

# Injection attacks only
powershell -File "Scripts/vapt_test_scripts.ps1" -TestInjection

# Rate limiting only
powershell -File "Scripts/vapt_test_scripts.ps1" -TestRateLimit
```

### Custom API URL
```bash
# C# Framework
dotnet run https://your-api-url.com

# PowerShell
powershell -File "Scripts/vapt_test_scripts.ps1" -BaseUrl "https://your-api-url.com"

# Bash
./Scripts/vapt_test_curl.sh https://your-api-url.com
```

## 📞 Need Help?

1. **Check Logs**: Look at the generated log files
2. **Verify API**: Ensure API is running and accessible
3. **Read Full Guide**: See `VAPT_TESTING_GUIDE.md` for detailed instructions
4. **Manual Testing**: Use Postman collection for step-by-step testing

---

**🎉 You're ready to test! Choose your preferred method above and start securing your API.** 