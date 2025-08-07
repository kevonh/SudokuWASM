@echo off
echo Building and serving Sudoku PWA for offline testing...
echo.

REM Build the release version
echo [1/3] Building release version...
dotnet publish -c Release -o publish
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo [2/3] Starting local server...
echo.
echo The app will be available at: http://localhost:8080
echo.
echo To test offline functionality:
echo 1. Open the app and let it load completely
echo 2. Open browser DevTools ^> Network tab ^> Check "Offline"
echo 3. Refresh the page - it should work offline!
echo.
echo Press Ctrl+C to stop the server
echo.

REM Start a simple HTTP server on the published content
cd publish\wwwroot
python -m http.server 8080 2>nul || python3 -m http.server 8080 2>nul || (
    echo Python not found. Please install Python or use another HTTP server.
    echo You can serve the files from: %CD%
    pause
)