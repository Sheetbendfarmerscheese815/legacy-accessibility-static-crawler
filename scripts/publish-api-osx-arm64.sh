#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
  VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)"
fi

package_version() {
  local package="$1"
  sed -n "s:.*Include=\"$package\" Version=\"\\([^\"]*\\)\".*:\\1:p" src/LegacyAccessibilityCrawler.Infrastructure/LegacyAccessibilityCrawler.Infrastructure.csproj | head -n 1
}

copy_required_driver() {
  local source="$1"
  local destination="$2"
  if [[ ! -f "$source" ]]; then
    echo "Required bundled browser driver was not found: $source" >&2
    exit 1
  fi
  cp -f "$source" "$destination"
  chmod +x "$destination/$(basename "$source")"
}

RUNTIME="osx-arm64"
SOURCE_REVISION_ID="${GITHUB_SHA:-}"
SOURCE_REVISION_ID="${SOURCE_REVISION_ID:0:7}"
BUILD_DATE_UTC="${BUILD_DATE_UTC:-$(date -u +%Y-%m-%dT%H:%M:%SZ)}"
NUGET_ROOT="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
CHROME_DRIVER_VERSION="$(package_version "Selenium.WebDriver.ChromeDriver")"
EDGE_DRIVER_VERSION="$(package_version "Selenium.WebDriver.MSEdgeDriver")"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
PROJECT="src/LegacyAccessibilityCrawler.Api/LegacyAccessibilityCrawler.Api.csproj"
PUBLISH_DIR="artifacts/publish/api-$RUNTIME"
RELEASE_DIR="artifacts/releases"
ARCHIVE="$RELEASE_DIR/legacy-accessibility-static-crawler-api-$VERSION-$RUNTIME.tar.gz"

echo "Publishing legacy-accessibility-static-crawler API/UI $VERSION for $RUNTIME"
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR" "$RELEASE_DIR"

dotnet restore legacy-accessibility-static-crawler.sln
dotnet build legacy-accessibility-static-crawler.sln -c Release --no-restore /p:Version="$VERSION" /p:SourceRevisionId="$SOURCE_REVISION_ID" /p:BuildDateUtc="$BUILD_DATE_UTC"
dotnet test legacy-accessibility-static-crawler.sln -c Release --no-build

dotnet publish "$PROJECT" \
  -c Release \
  -r "$RUNTIME" \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:EnableCompressionInSingleFile=true \
  /p:PublishTrimmed=false \
  /p:Version="$VERSION" \
  /p:SourceRevisionId="$SOURCE_REVISION_ID" \
  /p:BuildDateUtc="$BUILD_DATE_UTC" \
  -o "$PUBLISH_DIR"

copy_required_driver "$NUGET_ROOT/selenium.webdriver.chromedriver/$CHROME_DRIVER_VERSION/driver/mac64arm/chromedriver" "$PUBLISH_DIR"
copy_required_driver "$NUGET_ROOT/selenium.webdriver.msedgedriver/$EDGE_DRIVER_VERSION/driver/mac64/msedgedriver" "$PUBLISH_DIR"
cp -f README.md LICENSE VERSION.txt CHANGELOG.md "$PUBLISH_DIR/"
cp -R docs samples "$PUBLISH_DIR/"
cp -f samples/sample-config.json "$PUBLISH_DIR/appsettings.example.json" 2>/dev/null || true

rm -f "$ARCHIVE"
tar -czf "$ARCHIVE" -C "$PUBLISH_DIR" .

echo "Release archive: $ARCHIVE"
