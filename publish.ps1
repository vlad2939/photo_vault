# Publică versiunea portabilă PhotoVault (§8): un singur PhotoVault.exe self-contained (win-x64)
# + README.txt, în publish\PhotoVault\, plus arhiva publish\PhotoVault-<versiune>-win-x64.zip.
# Rulare (din rădăcina repo-ului, pe Windows cu .NET 10 SDK):  .\publish.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$out = Join-Path $PSScriptRoot 'publish\PhotoVault'
if (Test-Path $out) { Remove-Item -Recurse -Force $out }

dotnet publish PhotoVault.App -p:PublishProfile=Portabil
if ($LASTEXITCODE -ne 0) { throw "dotnet publish a eșuat." }

$version = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.Version
$zip = Join-Path $PSScriptRoot "publish\PhotoVault-$version-win-x64.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path $out -DestinationPath $zip

$exe = Get-Item (Join-Path $out 'PhotoVault.exe')
Write-Host ("PhotoVault.exe: {0:N1} MB" -f ($exe.Length / 1MB))
Write-Host "Folder portabil: $out"
Write-Host "Arhivă:          $zip"
