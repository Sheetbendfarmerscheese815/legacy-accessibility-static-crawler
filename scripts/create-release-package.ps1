param(
  [string]$Version = ""
)

$ErrorActionPreference = "Stop"
& "$PSScriptRoot/publish-all.ps1" -Version $Version
