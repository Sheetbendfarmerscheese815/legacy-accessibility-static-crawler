param(
  [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

if (!$Version) {
  [xml]$props = Get-Content "Directory.Build.props"
  $Version = $props.Project.PropertyGroup.Version
}

$ReleaseDir = "artifacts/releases"
Remove-Item $ReleaseDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

Write-Host "Publishing all release packages for version $Version"

& "$PSScriptRoot/publish-win-x64.ps1" -Version $Version
& bash "$PSScriptRoot/publish-linux-x64.sh" $Version
& bash "$PSScriptRoot/publish-osx-arm64.sh" $Version

$archives = Get-ChildItem $ReleaseDir -File | Where-Object { $_.Name -match '\.(zip|tar\.gz)$' } | Sort-Object Name
$checksumLines = foreach ($archive in $archives) {
  $hash = (Get-FileHash $archive.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
  "$hash  $($archive.Name)"
}

$checksumPath = Join-Path $ReleaseDir "SHA256SUMS.txt"
$checksumLines | Set-Content $checksumPath

Write-Host "Release artifacts:"
$archives | ForEach-Object { Write-Host " - $($_.FullName)" }
Write-Host " - $((Resolve-Path $checksumPath).Path)"
