@echo off
chcp 65001 >nul
title SR3H MACRO - Build Installer

echo.
echo ========================================================
echo   Building SR3H MACRO Final Installer Package
echo ========================================================
echo.

REM Run PowerShell script
PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0Build_Installer_Clean.ps1"

echo.
echo Press any key to exit...
pause >nul