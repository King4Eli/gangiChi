@echo off
setlocal EnableDelayedExpansion

rem Installs the gangiChi signing certificate(s) into Local Machine\Trusted Root Certification Authorities.
rem Usage: install_cert.bat [path\to\cert.cer]
rem With no argument, every .cer found under this script's folder is installed.

rem --- Re-launch elevated if not running as administrator ---
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%*' -Verb RunAs"
    exit /b
)

set "FOUND=0"
set "FAILED=0"

if not "%~1"=="" (
    call :install "%~f1"
) else (
    for /r "%~dp0" %%F in (*.cer) do call :install "%%~fF"
)

echo.
if "%FOUND%"=="0" (
    echo No .cer files found.
    set "FAILED=1"
) else if "%FAILED%"=="0" (
    echo Done. Certificate^(s^) installed to Trusted Root.
) else (
    echo Finished with errors.
)
pause
exit /b %FAILED%

:install
set /a FOUND+=1
echo Installing %~nx1 ...
if not exist "%~1" (
    echo   File not found: %~1
    set "FAILED=1"
    exit /b
)
certutil -addstore -f Root "%~1"
if errorlevel 1 (
    echo   Failed to install %~nx1
    set "FAILED=1"
) else (
    echo   Installed %~nx1
)
exit /b
