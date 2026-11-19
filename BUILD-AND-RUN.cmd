@echo off
setlocal EnableExtensions
cd /d "%~dp0"
title Siam Network Monitor - Build and Run
color 0B
cls
echo ============================================================
echo   SIAM NETWORK MONITOR - BUILD AND RUN
 echo ============================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 goto :nosdk

echo [1/3] .NET SDK found:
dotnet --version
echo.
echo [2/3] Stopping any running Siam Network Monitor...
taskkill /F /IM SiamNetworkMonitor.exe >nul 2>nul
timeout /t 2 /nobreak >nul
echo Restoring and building Release...
dotnet restore
echo.
if errorlevel 1 goto :failed
dotnet build -c Release --no-restore
if errorlevel 1 goto :failed

echo.
echo [3/3] Build succeeded.
set "APP=%CD%\bin\Release\net8.0-windows\SiamNetworkMonitor.exe"
if not exist "%APP%" goto :noexe
echo Starting:
echo %APP%
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%APP%' -Verb RunAs -Wait"
echo.
echo Siam Network Monitor has closed.
echo If the app disappeared immediately, send build-result.txt and app-error.txt.
echo.
pause
exit /b 0

:nosdk
echo ERROR: .NET 8 SDK was not found.
echo Install the SDK, not only the Desktop Runtime:
echo https://dotnet.microsoft.com/download/dotnet/8.0
echo.
pause
exit /b 1

:noexe
echo ERROR: Executable was not created at:
echo %APP%
echo.
pause
exit /b 1

:failed
echo.
echo BUILD FAILED. Copy or photograph the error shown above.
echo.
pause
exit /b 1
