@echo off
setlocal
echo ==========================================
echo GlassClock - Original Design + Battery
echo ==========================================
where dotnet >nul 2>&1
if errorlevel 1 (
  echo ERROR: .NET SDK was not found. Install the .NET 8 SDK first.
  pause
  exit /b 1
)
cd /d "%~dp0GlassClock"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Keep this window open and copy the full error output.
  pause
  exit /b 1
)
echo.
echo Build succeeded.
echo EXE: %CD%\bin\Release\net8.0-windows\win-x64\publish\GlassClock.exe
echo.
set /p RUNNOW="Launch GlassClock now? [Y/N]: "
if /I "%RUNNOW%"=="Y" start "" "%CD%\bin\Release\net8.0-windows\win-x64\publish\GlassClock.exe"
pause
