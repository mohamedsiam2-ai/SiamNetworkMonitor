$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Get-Process SiamNetworkMonitor -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

dotnet restore
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false --no-restore

$releaseRoot = Join-Path $root 'releases'
$staging = Join-Path $releaseRoot 'SiamNetworkMonitor-v18.0.0-win-x64'
Remove-Item $releaseRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item $staging -ItemType Directory -Force | Out-Null

$publish = Join-Path $root 'bin\Release\net8.0-windows\win-x64\publish'
Copy-Item "$publish\*" $staging -Recurse
Copy-Item README.md,CHANGELOG.md,PRIVACY.md,SECURITY.md -Destination $staging -ErrorAction SilentlyContinue

$zip = Join-Path $releaseRoot 'SiamNetworkMonitor-v18.0.0-win-x64.zip'
Compress-Archive "$staging\*" $zip -Force
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
"$hash  SiamNetworkMonitor-v18.0.0-win-x64.zip" | Set-Content (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ascii

Write-Host "Created: $zip" -ForegroundColor Green
Write-Host "SHA-256: $hash" -ForegroundColor Green
