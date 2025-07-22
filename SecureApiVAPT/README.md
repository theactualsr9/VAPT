# Secure API VAPT - .NET Core

A comprehensive .NET Core Web API with industry-standard VAPT (Vulnerability Assessment and Penetration Testing) security features implemented.

## 🛡️ Security Features Implemented

### 1. **Authentication & Authorization**
- JWT Bearer Token Authentication
- Role-based Authorization (Admin, User)
- Password Hashing (SHA256)
- Token-based Session Management

### 2. **Input Validation & Sanitization**
- Data Annotations for model validation
- Regular expression validation for passwords
- Email format validation
- Range and length validations

### 3. **Exception Handling**
- Global exception handling middleware
- Structured error responses
- Environment-aware error details (dev vs production)
- Comprehensive logging

### 4. **Rate Limiting**
- IP-based rate limiting
- Configurable limits (30 requests per minute)
- Custom rate limit responses

### 5. **Security Headers**
- X-Frame-Options: DENY (prevents clickjacking)
- X-XSS-Protection: 1; mode=block
- X-Content-Type-Options: nosniff
- Strict-Transport-Security: max-age=31536000
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy: geolocation=(), microphone=(), camera=()
- Content-Security-Policy

### 6. **CORS Policy**
- Restricted origins
- Configurable headers and methods

### 7. **HTTPS Enforcement**
- Automatic HTTPS redirection
- HSTS headers

### 8. **Logging & Monitoring**
- Serilog structured logging
- Console and file logging
- Request/response logging
- Security event logging

### 9. **API Versioning**
- URL segment versioning
- Version reporting
- Backward compatibility support

## 🏗️ Project Structure

```
SecureApiVAPT/
├── Controllers/           # API Controllers
│   ├── AuthController.cs
│   ├── UsersController.cs
│   └── ProductsController.cs
├── DTOs/                  # Data Transfer Objects
│   └── UserDto.cs
├── Models/                # Domain Models
│   └── User.cs
├── Services/              # Business Logic Layer
│   ├── IUserService.cs
│   ├── UserService.cs
│   ├── IProductService.cs
│   └── ProductService.cs
├── Middleware/            # Custom Middleware
│   ├── ExceptionHandlingMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Extensions/            # Extension Methods
│   ├── ServiceCollectionExtensions.cs
│   └── ApplicationBuilderExtensions.cs
├── Configuration/         # Configuration Classes
├── Program.cs            # Application Entry Point
└── appsettings.json      # Configuration
```

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd SecureApiVAPT
   ```

2. **Update Configuration**
   Edit `appsettings.json`:
   ```json
   {
     "Jwt": {
       "Key": "your-super-secret-key-here-minimum-16-characters",
       "Issuer": "your-application-name",
       "Audience": "your-application-audience"
     },
     "IpRateLimiting": {
       "GeneralRules": [
         {
           "Endpoint": "*",
           "Period": "1m",
           "Limit": 30
         }
       ]
     }
   }
   ```

3. **Update CORS Policy**
   In `Extensions/ServiceCollectionExtensions.cs`, update the allowed origins:
   ```csharp
   builder.WithOrigins("https://yourdomain.com", "https://localhost:3000")
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

## 📋 API Endpoints

### Authentication Endpoints

#### POST `/api/v1/auth/register`
Register a new user
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "age": 25
}
```

#### POST `/api/v1/auth/login`
Login user
```json
{
  "username": "john_doe",
  "password": "SecurePass123!"
}
```

#### POST `/api/v1/auth/change-password` (Requires Auth)
Change user password
```json
{
  "currentPassword": "SecurePass123!",
  "newPassword": "NewSecurePass456!"
}
```

#### GET `/api/v1/auth/profile` (Requires Auth)
Get user profile

### User Management Endpoints (Admin Only)

#### GET `/api/v1/users`
Get all users

#### GET `/api/v1/users/{id}`
Get user by ID

#### PUT `/api/v1/users/{id}`
Update user

#### DELETE `/api/v1/users/{id}`
Delete user

#### GET `/api/v1/users/search?username={username}&email={email}`
Search users

### Product Endpoints

#### GET `/api/v1/products`
Get all products

#### GET `/api/v1/products/{id}`
Get product by ID

#### POST `/api/v1/products` (Admin Only)
Create new product
```json
{
  "name": "Sample Product",
  "price": 29.99,
  "description": "Product description",
  "stockQuantity": 100
}
```

#### PUT `/api/v1/products/{id}` (Admin Only)
Update product

#### DELETE `/api/v1/products/{id}` (Admin Only)
Delete product

#### GET `/api/v1/products/search?searchTerm={term}`
Search products

#### GET `/api/v1/products/category/{category}`
Get products by category

#### PATCH `/api/v1/products/{id}/stock` (Admin Only)
Update product stock
```json
{
  "quantity": 10
}
```

## 🔐 Security Testing

### VAPT Testing Checklist

1. **Authentication Testing**
   - Test JWT token validation
   - Test expired tokens
   - Test invalid tokens
   - Test role-based access

2. **Input Validation Testing**
   - Test SQL injection attempts
   - Test XSS payloads
   - Test malformed JSON
   - Test oversized payloads

3. **Rate Limiting Testing**
   - Test rate limit enforcement
   - Test rate limit bypass attempts

4. **Authorization Testing**
   - Test unauthorized access
   - Test privilege escalation
   - Test role-based restrictions

5. **Error Handling Testing**
   - Test exception handling
   - Verify no sensitive data leakage
   - Test malformed requests

### Testing Tools
- **OWASP ZAP**: Automated security testing
- **Burp Suite**: Manual security testing
- **Postman**: API testing
- **curl**: Command-line testing

## 📊 Monitoring & Logging

### Log Files
- Application logs: `logs/log.txt`
- Daily rolling log files
- Structured JSON logging

### Security Events Logged
- User registration/login
- Failed authentication attempts
- Admin actions
- Rate limit violations
- Exception occurrences

## 🔧 Configuration

### Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://localhost:5001
```

### Security Headers Configuration
Modify `SecurityHeadersMiddleware.cs` to customize security headers.

### Rate Limiting Configuration
Update `appsettings.json` to modify rate limiting rules.

## 🚨 Security Best Practices

1. **Never commit secrets to source control**
2. **Use strong, unique JWT keys**
3. **Regularly update dependencies**
4. **Monitor logs for suspicious activity**
5. **Use HTTPS in production**
6. **Implement proper CORS policies**
7. **Validate all inputs**
8. **Use parameterized queries (when using database)**
9. **Implement proper session management**
10. **Regular security audits**

## 📝 License

This project is licensed under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📞 Support

For security issues, please contact the security team directly. 