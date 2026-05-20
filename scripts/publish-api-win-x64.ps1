param(
  [string]$Version = "",
  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

function Get-ProjectVersion {
  if ($Version) { return $Version }
  [xml]$props = Get-Content "Directory.Build.props"
  return $props.Project.PropertyGroup.Version
}

function Copy-ReleaseContent {
  param([string]$PublishDir)
  Copy-Item README.md $PublishDir -Force
  Copy-Item LICENSE $PublishDir -Force
  Copy-Item VERSION.txt $PublishDir -Force
  Copy-Item CHANGELOG.md $PublishDir -Force
  Copy-Item docs $PublishDir -Recurse -Force
  Copy-Item samples $PublishDir -Recurse -Force
  if (Test-Path "samples/sample-config.json") {
    Copy-Item "samples/sample-config.json" (Join-Path $PublishDir "appsettings.example.json") -Force
  }
}

function Get-PackageVersion {
  param([string]$PackageName)
  [xml]$project = Get-Content "src/LegacyAccessibilityCrawler.Infrastructure/LegacyAccessibilityCrawler.Infrastructure.csproj"
  return ($project.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $PackageName } | Select-Object -First 1).Version
}

function Copy-RequiredDriver {
  param(
    [string]$Source,
    [string]$Destination
  )
  if (!(Test-Path $Source)) {
    throw "Required bundled browser driver was not found: $Source"
  }
  Copy-Item $Source $Destination -Force
}

$Version = Get-ProjectVersion
$SourceRevisionId = if ($env:GITHUB_SHA) { $env:GITHUB_SHA.Substring(0, [Math]::Min(7, $env:GITHUB_SHA.Length)) } else { "" }
$BuildDateUtc = if ($env:BUILD_DATE_UTC) { $env:BUILD_DATE_UTC } else { (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") }
$env:DOTNET_ROLL_FORWARD = if ($env:DOTNET_ROLL_FORWARD) { $env:DOTNET_ROLL_FORWARD } else { "Major" }
$NuGetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME ".nuget/packages" }
$ChromeDriverVersion = Get-PackageVersion "Selenium.WebDriver.ChromeDriver"
$EdgeDriverVersion = Get-PackageVersion "Selenium.WebDriver.MSEdgeDriver"
$Runtime = "win-x64"
$Project = "src/LegacyAccessibilityCrawler.Api/LegacyAccessibilityCrawler.Api.csproj"
$PublishDir = "artifacts/publish/api-$Runtime"
$ReleaseDir = "artifacts/releases"
$Archive = "$ReleaseDir/legacy-accessibility-static-crawler-api-$Version-$Runtime.zip"

Write-Host "Publishing legacy-accessibility-static-crawler API/UI $Version for $Runtime"
Remove-Item $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $PublishDir, $ReleaseDir -Force | Out-Null

if (!$SkipBuild) {
  dotnet restore legacy-accessibility-static-crawler.sln
  dotnet build legacy-accessibility-static-crawler.sln -c Release --no-restore /p:Version=$Version /p:SourceRevisionId=$SourceRevisionId /p:BuildDateUtc=$BuildDateUtc
  dotnet test legacy-accessibility-static-crawler.sln -c Release --no-build
}

dotnet publish $Project `
  -c Release `
  -r $Runtime `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:PublishTrimmed=false `
  /p:Version=$Version `
  /p:SourceRevisionId=$SourceRevisionId `
  /p:BuildDateUtc=$BuildDateUtc `
  -o $PublishDir

Copy-RequiredDriver (Join-Path $NuGetRoot "selenium.webdriver.chromedriver/$ChromeDriverVersion/driver/win32/chromedriver.exe") $PublishDir
Copy-RequiredDriver (Join-Path $NuGetRoot "selenium.webdriver.msedgedriver/$EdgeDriverVersion/driver/win64/msedgedriver.exe") $PublishDir
Copy-ReleaseContent $PublishDir

Remove-Item $Archive -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$PublishDir/*" -DestinationPath $Archive
$Hash = (Get-FileHash $Archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$Hash  $(Split-Path $Archive -Leaf)" | Set-Content "$ReleaseDir/SHA256SUMS-api-$Runtime.txt"

Write-Host "Release archive: $Archive"
