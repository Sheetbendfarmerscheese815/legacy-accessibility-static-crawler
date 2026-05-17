#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

VERSION="${1:-}"
if [[ -z "$VERSION" ]]; then
  VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)"
fi

RUNTIME="linux-x64"
SOURCE_REVISION_ID="${GITHUB_SHA:-}"
SOURCE_REVISION_ID="${SOURCE_REVISION_ID:0:7}"
BUILD_DATE_UTC="${BUILD_DATE_UTC:-$(date -u +%Y-%m-%dT%H:%M:%SZ)}"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"
PROJECT="src/LegacyAccessibilityCrawler.Cli/LegacyAccessibilityCrawler.Cli.csproj"
PUBLISH_DIR="artifacts/publish/$RUNTIME"
RELEASE_DIR="artifacts/releases"
ARCHIVE="$RELEASE_DIR/legacy-accessibility-static-crawler-$VERSION-$RUNTIME.tar.gz"

echo "Publishing legacy-accessibility-static-crawler $VERSION for $RUNTIME"
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

cp -f "src/LegacyAccessibilityCrawler.Cli/bin/Release/net8.0/$RUNTIME/chromedriver" "$PUBLISH_DIR/" 2>/dev/null || true
cp -f "src/LegacyAccessibilityCrawler.Cli/bin/Release/net8.0/$RUNTIME/msedgedriver" "$PUBLISH_DIR/" 2>/dev/null || true
cp -f README.md LICENSE VERSION.txt CHANGELOG.md "$PUBLISH_DIR/"
cp -R docs samples "$PUBLISH_DIR/"
cp -f samples/sample-config.json "$PUBLISH_DIR/appsettings.example.json" 2>/dev/null || true

rm -f "$ARCHIVE"
tar -czf "$ARCHIVE" -C "$PUBLISH_DIR" .

if command -v shasum >/dev/null 2>&1; then
  shasum -a 256 "$ARCHIVE" | awk '{print $1 "  " $2}' | sed "s#  $RELEASE_DIR/#  #" > "$RELEASE_DIR/SHA256SUMS.txt"
else
  sha256sum "$ARCHIVE" | sed "s#  $RELEASE_DIR/#  #" > "$RELEASE_DIR/SHA256SUMS.txt"
fi

echo "Release archive: $ARCHIVE"
echo "Checksum file: $RELEASE_DIR/SHA256SUMS.txt"
