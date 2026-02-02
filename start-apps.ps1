# Start Backend
Write-Host "Starting Backend..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\FlightBackend\Flightfront.Presentation'; dotnet run --launch-profile http"

# Vänta lite så backend hinner starta
Start-Sleep -Seconds 3

# Start Frontend
Write-Host "Starting Frontend..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\FlightFrontend'; ng serve"

Write-Host "`nBoth applications are starting..." -ForegroundColor Cyan
Write-Host "Backend: http://localhost:5018" -ForegroundColor Yellow
Write-Host "Frontend: http://localhost:4200" -ForegroundColor Yellow
