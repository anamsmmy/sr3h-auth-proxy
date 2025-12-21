@echo off
REM SR3H MACRO - Build and Protection Script
REM This script builds the application and applies obfuscation/protection

setlocal enabledelayedexpansion

echo.
echo =====================================
echo 🔐 SR3H MACRO - Build ^& Protection
echo =====================================
echo.

REM Check if PowerShell is available
powershell -Command "exit" >nul 2>&1
if errorlevel 1 (
    echo ❌ PowerShell is required but not found!
    pause
    exit /b 1
)

REM Execute the PowerShell script
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-and-protect.ps1" %*

if errorlevel 1 (
    echo.
    echo ❌ Script execution failed!
    pause
    exit /b 1
)

echo.
echo ✅ Protection completed successfully!
echo.
pause
