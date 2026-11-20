$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
Get-Process SiamNetworkMonitor -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false --no-restore
$out=Join-Path $root 'releases'
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
New-Item $out -ItemType Directory | Out-Null
$publish=Join-Path $root 'bin\Release\net8.0-windows\win-x64\publish'
Copy-Item "$publish\*" $out -Recurse
Copy-Item README.md,CHANGELOG.md -Destination $out -ErrorAction SilentlyContinue
$zip=Join-Path $root 'SiamNetworkMonitor-v18.0.0-rc1-win-x64.zip'
Compress-Archive "$out\*" $zip -Force
(Get-FileHash $zip -Algorithm SHA256).Hash | Set-Content "$zip.sha256.txt"
Write-Host "Created $zip" -ForegroundColor Green
