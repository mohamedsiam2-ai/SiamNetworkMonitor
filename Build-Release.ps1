$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$log = Join-Path $root 'build-result.txt'
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 8 SDK is not installed. Install the SDK from https://dotnet.microsoft.com/download/dotnet/8.0' }
    Get-Process SiamNetworkMonitor -ErrorAction SilentlyContinue | Stop-Process -Force
    & dotnet restore 2>&1 | Tee-Object -FilePath $log
    if ($LASTEXITCODE) { throw 'dotnet restore failed. See build-result.txt.' }
    & dotnet build -c Release --no-restore 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE) { throw 'dotnet build failed. See build-result.txt.' }
    $exe = Join-Path $root 'bin\Release\net8.0-windows\SiamNetworkMonitor.exe'
    Start-Process $exe -Verb RunAs
    Write-Host "Built and started: $exe" -ForegroundColor Green
} catch {
    $_ | Out-String | Tee-Object -FilePath $log -Append
    Write-Host $_ -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}
