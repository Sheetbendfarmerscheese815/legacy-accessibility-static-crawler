#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-1.0.0}"

dotnet publish src/LegacyAccessibilityCrawler.Api/LegacyAccessibilityCrawler.Api.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  /p:Version="$VERSION" \
  -o artifacts/legacy-a11y-api-osx-arm64

cp -f src/LegacyAccessibilityCrawler.Api/bin/Release/net8.0/osx-arm64/chromedriver artifacts/legacy-a11y-api-osx-arm64/ 2>/dev/null || true
cp -f src/LegacyAccessibilityCrawler.Api/bin/Release/net8.0/osx-arm64/msedgedriver artifacts/legacy-a11y-api-osx-arm64/ 2>/dev/null || true
