@echo off
setlocal enableextensions
cd /d "%~dp0"

call :isAdmin
if %errorlevel% == 0 (
%WinDir%\Microsoft.NET\Framework\v4.0.30319\installutil /u "nntpAutoPosterWindowsService.exe"
) else (
ECHO run as Administrator
)

pause
exit /b

:isAdmin
fsutil dirty query %systemdrive% >nul
exit /b