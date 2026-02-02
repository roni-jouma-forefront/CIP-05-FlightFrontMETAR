# FlightFront METAR - Start Frontend and Backend
# This script starts both the Angular frontend and ASP.NET Core backend

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FlightFront METAR Application Launcher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$backendPath = "$PSScriptRoot\FlightBackend\Flightfront.Presentation"
$frontendPath = "$PSScriptRoot\FlightFrontend"

# Backend configuration
$backendPort = "5018"
$backendUrl = "http://localhost:$backendPort"

# Frontend configuration
$frontendPort = "4200"
$frontendUrl = "http://localhost:$frontendPort"

# Verify paths exist
if (-not (Test-Path $backendPath)) {
    Write-Host "ERROR: Backend path not found: $backendPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $frontendPath)) {
    Write-Host "ERROR: Frontend path not found: $frontendPath" -ForegroundColor Red
    exit 1
}

# Function to cleanup on exit
function Cleanup {
    Write-Host ""
    Write-Host "Shutting down applications..." -ForegroundColor Yellow

    # Stop all jobs
    Get-Job | Stop-Job
    Get-Job | Remove-Job

    Write-Host "Applications stopped." -ForegroundColor Green
    exit
}

# Register cleanup on Ctrl+C
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Cleanup }

Write-Host "[1/4] Starting Backend API..." -ForegroundColor Yellow
Write-Host "      Path: $backendPath" -ForegroundColor Gray
Write-Host "      URL:  $backendUrl" -ForegroundColor Gray

# Start backend in a new PowerShell window
$backendJob = Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$backendPath'; Write-Host 'Starting ASP.NET Core Backend...' -ForegroundColor Cyan; dotnet run --launch-profile http"
) -PassThru

Start-Sleep -Seconds 3

Write-Host "[2/4] Waiting for Backend API to be ready..." -ForegroundColor Yellow

# Wait for backend to be ready (max 30 seconds)
$maxAttempts = 30
$attempt = 0
$backendReady = $false

while ($attempt -lt $maxAttempts -and -not $backendReady) {
    try {
        $response = Invoke-WebRequest -Uri "$backendUrl/api/metar/health" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 404) {
            $backendReady = $true
            Write-Host "      Backend API is ready!" -ForegroundColor Green
        }
    }
    catch {
        # Check if the backend is at least listening (even if endpoint doesn't exist)
        try {
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $tcpClient.Connect("localhost", $backendPort)
            $tcpClient.Close()
            $backendReady = $true
            Write-Host "      Backend API is ready!" -ForegroundColor Green
        }
        catch {
            $attempt++
            Write-Host "      Attempt $attempt/$maxAttempts - Waiting for backend..." -ForegroundColor Gray
            Start-Sleep -Seconds 1
        }
    }
}

if (-not $backendReady) {
    Write-Host "ERROR: Backend failed to start after $maxAttempts seconds" -ForegroundColor Red
    Cleanup
}

Write-Host "[3/4] Starting Frontend (Angular)..." -ForegroundColor Yellow
Write-Host "      Path: $frontendPath" -ForegroundColor Gray
Write-Host "      URL:  $frontendUrl" -ForegroundColor Gray

# Check if node_modules exists
if (-not (Test-Path "$frontendPath\node_modules")) {
    Write-Host "      Installing npm packages (first time setup)..." -ForegroundColor Cyan
    Push-Location $frontendPath
    npm install
    Pop-Location
}

# Start frontend in a new PowerShell window
$frontendJob = Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$frontendPath'; Write-Host 'Starting Angular Frontend...' -ForegroundColor Cyan; npm start"
) -PassThru

Write-Host "[4/4] Waiting for Frontend to be ready..." -ForegroundColor Yellow

# Wait for frontend to be ready (max 60 seconds)
$maxAttempts = 60
$attempt = 0
$frontendReady = $false

while ($attempt -lt $maxAttempts -and -not $frontendReady) {
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("localhost", $frontendPort)
        $tcpClient.Close()
        $frontendReady = $true
        Write-Host "      Frontend is ready!" -ForegroundColor Green
    }
    catch {
        $attempt++
        if ($attempt % 5 -eq 0) {
            Write-Host "      Attempt $attempt/$maxAttempts - Waiting for frontend..." -ForegroundColor Gray
        }
        Start-Sleep -Seconds 1
    }
}

if (-not $frontendReady) {
    Write-Host "WARNING: Frontend may still be starting..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Applications Started Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Backend API:  $backendUrl" -ForegroundColor Cyan
Write-Host "Swagger UI:   $backendUrl/swagger" -ForegroundColor Cyan
Write-Host "Frontend:     $frontendUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANT:" -ForegroundColor Yellow
Write-Host "- Frontend expects API at http://localhost:5018/api" -ForegroundColor Yellow
Write-Host "- CORS is configured to allow frontend requests" -ForegroundColor Yellow
Write-Host "- Both applications are running in separate windows" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press Ctrl+C in this window to stop monitoring" -ForegroundColor Magenta
Write-Host "Or close the application windows individually" -ForegroundColor Magenta
Write-Host ""

# Keep the script running and monitor processes
try {
    while ($true) {
        # Check if processes are still running
        if ($backendJob.HasExited) {
            Write-Host "WARNING: Backend process has stopped!" -ForegroundColor Red
        }
        if ($frontendJob.HasExited) {
            Write-Host "WARNING: Frontend process has stopped!" -ForegroundColor Red
        }

        Start-Sleep -Seconds 5
    }
}
finally {
    Cleanup
}
