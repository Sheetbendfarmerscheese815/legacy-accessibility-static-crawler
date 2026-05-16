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

Copy-Item src/LegacyAccessibilityCrawler.Cli/bin/Release/net8.0/win-x64/chromedriver.exe artifacts/legacy-a11y-crawler-win-x64/ -ErrorAction SilentlyContinue
Copy-Item src/LegacyAccessibilityCrawler.Cli/bin/Release/net8.0/win-x64/msedgedriver.exe artifacts/legacy-a11y-crawler-win-x64/ -ErrorAction SilentlyContinue

dotnet publish src/LegacyAccessibilityCrawler.Api/LegacyAccessibilityCrawler.Api.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:Version=$Version `
  -o artifacts/legacy-a11y-api-win-x64

Copy-Item src/LegacyAccessibilityCrawler.Api/bin/Release/net8.0/win-x64/chromedriver.exe artifacts/legacy-a11y-api-win-x64/ -ErrorAction SilentlyContinue
Copy-Item src/LegacyAccessibilityCrawler.Api/bin/Release/net8.0/win-x64/msedgedriver.exe artifacts/legacy-a11y-api-win-x64/ -ErrorAction SilentlyContinue
