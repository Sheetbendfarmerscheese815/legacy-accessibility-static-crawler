param(
  [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$packages = @(
  "legacy-a11y-crawler-win-x64",
  "legacy-a11y-crawler-linux-x64",
  "legacy-a11y-crawler-osx-arm64",
  "legacy-a11y-api-win-x64",
  "legacy-a11y-api-linux-x64",
  "legacy-a11y-api-osx-arm64"
)

foreach ($package in $packages) {
  $root = "artifacts/$package"
  if (!(Test-Path $root)) { continue }
  Copy-Item README.md $root -Force
  Copy-Item LICENSE $root -Force
  Copy-Item VERSION.txt $root -Force
  Copy-Item CHANGELOG.md $root -Force
  Copy-Item samples/sample-config.json "$root/appsettings.example.json" -Force
  Copy-Item docs $root -Recurse -Force
  Copy-Item samples $root -Recurse -Force
  $zip = "artifacts/$package-$Version.zip"
  if (Test-Path $zip) { Remove-Item $zip -Force }
  Compress-Archive -Path "$root/*" -DestinationPath $zip
}
