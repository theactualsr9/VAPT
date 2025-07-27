@echo off
REM VAPT Security Testing Batch File for Windows
REM This script provides an easy way to run VAPT tests on Windows

setlocal enabledelayedexpansion

REM Set default values
set BASE_URL=https://localhost:5001
set OUTPUT_DIR=vapt_results
set TIMESTAMP=%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%

REM Parse command line arguments
:parse_args
if "%~1"=="" goto :main
if /i "%~1"=="--url" (
    set BASE_URL=%~2
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--output" (
    set OUTPUT_DIR=%~2
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--help" (
    goto :show_help
)
shift
goto :parse_args

:show_help
echo VAPT Security Testing Tool
echo.
echo Usage: run_vapt_tests.bat [options]
echo.
echo Options:
echo   --url URL        Target API URL ^(default: https://localhost:5001^)
echo   --output DIR     Output directory ^(default: vapt_results^)
echo   --help          Show this help message
echo.
echo Examples:
echo   run_vapt_tests.bat
echo   run_vapt_tests.bat --url https://api.example.com
echo   run_vapt_tests.bat --output custom_results
echo.
pause
exit /b 0

:main
echo ========================================
echo    VAPT Security Testing Tool
echo ========================================
echo.
echo Target URL: %BASE_URL%
echo Output Directory: %OUTPUT_DIR%
echo Timestamp: %TIMESTAMP%
echo.

REM Create output directory
if not exist "%OUTPUT_DIR%" (
    echo Creating output directory: %OUTPUT_DIR%
    mkdir "%OUTPUT_DIR%"
)

REM Check if PowerShell is available
powershell -Command "Get-Host" >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: PowerShell is not available. Please install PowerShell 5.1 or later.
    pause
    exit /b 1
)

REM Check if the API is running
echo Checking if API is accessible...
curl -s -o nul -w "%%{http_code}" "%BASE_URL%/api/v1/products" > temp_status.txt
set /p STATUS_CODE=<temp_status.txt
del temp_status.txt

if "%STATUS_CODE%"=="000" (
    echo WARNING: API appears to be offline at %BASE_URL%
    echo Please ensure the API is running before proceeding.
    echo.
    set /p CONTINUE="Do you want to continue anyway? (y/N): "
    if /i not "!CONTINUE!"=="y" (
        echo Testing cancelled.
        pause
        exit /b 1
    )
) else (
    echo API is accessible ^(Status: %STATUS_CODE%^)
)

echo.
echo Starting VAPT Security Tests...
echo.

REM Run PowerShell script
powershell -ExecutionPolicy Bypass -File "%~dp0vapt_test_scripts.ps1" -BaseUrl "%BASE_URL%" -OutputPath "%OUTPUT_DIR%" -RunAll

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo    VAPT Testing Completed Successfully
    echo ========================================
    echo.
    echo Results saved to: %OUTPUT_DIR%
    echo Check the log files for detailed results.
) else (
    echo.
    echo ========================================
    echo    VAPT Testing Failed
    echo ========================================
    echo.
    echo Check the error messages above for details.
)

echo.
echo Press any key to exit...
pause >nul 