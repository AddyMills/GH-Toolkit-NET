@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"
title Fix startup issues

for %%a in ("%~dp0..") do set "parent=%%~fa"
set "len=0"
for /f "usebackq delims=" %%L in (`powershell -NoProfile -Command "Write-Output ('%parent%').Length"`) do set "len=%%L"

if %len% GEQ 46 (
    echo The path you extracted the game to is too long and will cause the game to crash when loading a song.
    echo.
    echo Current path:
    echo "%parent%"
    echo.
    echo Please move it folder to a shorter path, for example:
    echo "C:\Games\Guitar Hero III"
    echo.
    echo Then run this script again.
    echo.
    pause
    exit /b
)

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -NoProfile -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo ================================
echo      KEEP THIS WINDOW OPEN!
echo ================================
echo.

if exist "%USERPROFILE%\AppData\Local\Aspyr\Guitar Hero III" (
    rd /s /q "%USERPROFILE%\AppData\Local\Aspyr\Guitar Hero III"
    echo - Graphics configuration deleted
    echo - Online account signed out
)

reg import "Dependencies\Registry\Remove other GH3 install(s).reg"
echo - Old installs invalidated


REM =========================================================
REM Detect system UI language and choose matching .reg file
REM Supported: German, Spanish, Italian, French
REM Fallback: English
REM =========================================================

set "langRegFile=Dependencies\Registry\Change language to English.reg"
set "uiLang="

for /f "usebackq delims=" %%L in (`powershell -NoProfile -Command "$c=Get-UICulture; if($c){$c.Name}else{''}"`) do set "uiLang=%%L"

REM Normalize matching by checking prefix (ex: de-AT -> de)
set "uiPrefix=!uiLang:~0,2!"

if /I "!uiPrefix!"=="de" (
    set "langRegFile=Dependencies\Registry\Change language to German.reg"
) else if /I "!uiPrefix!"=="es" (
    set "langRegFile=Dependencies\Registry\Change language to Spanish.reg"
) else if /I "!uiPrefix!"=="it" (
    set "langRegFile=Dependencies\Registry\Change language to Italian.reg"
) else if /I "!uiPrefix!"=="fr" (
    set "langRegFile=Dependencies\Registry\Change language to French.reg"
)

if exist "!langRegFile!" (
    reg import "!langRegFile!"
    echo - Guitar Hero III language set using: "!langRegFile!"
    echo   Detected Windows UI language: "!uiLang!"
) else (
    echo - ERROR: Language registry file not found:
    echo   "!langRegFile!"
    echo.
    echo   Falling back to English...
    reg import "Dependencies\Registry\Change language to English.reg"
    echo - Guitar Hero III language set to English
)

echo.

echo Install DirectX 9
echo Please complete the setup, then this script will continue automatically.
echo.

start /wait "" "Dependencies\directx9\DXSETUP.exe"

echo Install Visual C++ Redistributable (x86)...
echo Please complete the setup, then this script will continue automatically.
echo.

start /wait "" "Dependencies\VC_redist.x86.exe"

cls
echo Done! You may now close this window.
echo.
pause
exit /b
