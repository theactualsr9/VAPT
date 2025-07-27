# VAPT Demo Script
# This script starts the API and runs VAPT tests

Write-Host "🚀 Starting VAPT Security Testing Demo" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Check if .NET is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET not found. Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Start the API in background
Write-Host "Starting Secure API..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", "http://localhost:5000" -PassThru -WindowStyle Hidden

# Wait for API to start
Write-Host "Waiting for API to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Test if API is running
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/products" -TimeoutSec 5
    Write-Host "✓ API is running successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ API failed to start properly" -ForegroundColor Red
    Write-Host "Trying to start API manually..." -ForegroundColor Yellow
    
    # Kill any existing process
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {$_.ProcessName -eq "dotnet"} | Stop-Process -Force
    
    # Start API manually
    Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", "http://localhost:5000" -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 15
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/products" -TimeoutSec 5
        Write-Host "✓ API is now running" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to start API. Please check the application logs." -ForegroundColor Red
        exit 1
    }
}

# Run VAPT Tests
Write-Host "Running VAPT Security Tests..." -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Green

try {
    # Change to VAPTTests directory and run tests
    Set-Location "VAPTTests"
    dotnet run "http://localhost:5000"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ VAPT tests completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ VAPT tests failed" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error running VAPT tests: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Return to original directory
    Set-Location ".."
}

# Stop the API
Write-Host "Stopping API..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {$_.ProcessName -eq "dotnet"} | Stop-Process -Force

Write-Host "Demo completed!" -ForegroundColor Green
Write-Host "Check the VAPTTests directory for test results and reports." -ForegroundColor Cyan 