#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-1.0.0}"

dotnet publish src/LegacyAccessibilityCrawler.Cli/LegacyAccessibilityCrawler.Cli.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:EnableCompressionInSingleFile=true \
  /p:Version="$VERSION" \
  -o artifacts/legacy-a11y-crawler-osx-arm64
