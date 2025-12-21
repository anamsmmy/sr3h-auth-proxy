@echo off
chcp 65001 >nul
title SR3H MACRO - Build Installer

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║         🚀 بناء ملف تثبيت SR3H MACRO النهائي 🚀          ║
echo ║      Building SR3H MACRO Final Installer Package         ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

REM تشغيل سكريبت PowerShell
PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0Build_Final_Installer.ps1"

echo.
echo اضغط أي مفتاح للخروج...
echo Press any key to exit...
pause >nul