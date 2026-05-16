param(
  [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
dotnet publish src/LegacyAccessibilityCrawler.Cli/LegacyAccessibilityCrawler.Cli.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:Version=$Version `
  -o artifacts/legacy-a11y-crawler-win-x64

dotnet publish src/LegacyAccessibilityCrawler.Api/LegacyAccessibilityCrawler.Api.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:Version=$Version `
  -o artifacts/legacy-a11y-api-win-x64
