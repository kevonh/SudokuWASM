#!/usr/bin/env pwsh

Write-Host "Building and serving Sudoku PWA for offline testing..." -ForegroundColor Green
Write-Host ""

# Build the release version
Write-Host "[1/3] Building release version..." -ForegroundColor Yellow
dotnet publish -c Release -o publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "[2/3] Starting local server..." -ForegroundColor Yellow
Write-Host ""
Write-Host "The app will be available at: " -NoNewline
Write-Host "http://localhost:8080" -ForegroundColor Cyan
Write-Host ""
Write-Host "To test offline functionality:" -ForegroundColor Green
Write-Host "1. Open the app and let it load completely"
Write-Host "2. Open browser DevTools > Network tab > Check 'Offline'"
Write-Host "3. Refresh the page - it should work offline!"
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Change to publish directory
Set-Location "publish\wwwroot"

# Try to start HTTP server
try {
    # Try Python 3 first, then Python 2
    if (Get-Command python3 -ErrorAction SilentlyContinue) {
        python3 -m http.server 8080
    } elseif (Get-Command python -ErrorAction SilentlyContinue) {
        python -m http.server 8080
    } else {
        Write-Host "Python not found. Trying alternative methods..." -ForegroundColor Yellow
        
        # Try dotnet serve if available
        if (Get-Command dotnet -ErrorAction SilentlyContinue) {
            try {
                dotnet tool install --global dotnet-serve --quiet 2>$null
                dotnet serve -p 8080 --quiet
            } catch {
                Write-Host "Please install Python or use another HTTP server." -ForegroundColor Red
                Write-Host "You can serve the files from: $(Get-Location)" -ForegroundColor Cyan
                Read-Host "Press Enter to exit"
            }
        }
    }
} catch {
    Write-Host "Failed to start server: $_" -ForegroundColor Red
    Write-Host "You can serve the files from: $(Get-Location)" -ForegroundColor Cyan
    Read-Host "Press Enter to exit"
}