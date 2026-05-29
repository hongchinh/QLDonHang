$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendDir = Join-Path $ScriptDir "backend\src\OrderMgmt.WebApi"
$FrontendDir = Join-Path $ScriptDir "frontend"

Write-Host "Khoi dong QL Don Hang..." -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "LOI: Chua cai .NET SDK" -ForegroundColor Red; exit 1
}
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "LOI: Chua cai Node.js" -ForegroundColor Red; exit 1
}

if (-not (Test-Path (Join-Path $FrontendDir "node_modules"))) {
    Write-Host "Cai dat frontend dependencies..." -ForegroundColor Yellow
    Push-Location $FrontendDir; npm install; Pop-Location
}

# Chay backend trong background (cung terminal, output hien thi xen ke)
Write-Host "Backend  -> http://localhost:5050/swagger" -ForegroundColor Green
$backend = Start-Process dotnet `
    -ArgumentList "run --no-launch-profile --urls http://localhost:5050" `
    -WorkingDirectory $BackendDir `
    -NoNewWindow -PassThru

Write-Host "Frontend -> http://localhost:5173" -ForegroundColor Green
Write-Host "(Ctrl+C de dung ca hai)" -ForegroundColor Gray
Write-Host ""

try {
    Set-Location $FrontendDir
    npm run dev
} finally {
    Write-Host "`nDang dung backend..." -ForegroundColor Yellow
    if (-not $backend.HasExited) {
        Stop-Process -Id $backend.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Da dung." -ForegroundColor Green
}
