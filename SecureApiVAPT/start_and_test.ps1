# Simple script to start API and run VAPT tests

Write-Host "Starting Secure API..." -ForegroundColor Yellow
Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", "http://localhost:5000" -WindowStyle Hidden

Write-Host "Waiting for API to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host "Running VAPT Tests..." -ForegroundColor Green
Set-Location "VAPTTests"
dotnet run "http://localhost:5000"

Write-Host "Stopping API..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Done!" -ForegroundColor Green 