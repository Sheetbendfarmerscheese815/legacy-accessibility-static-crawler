# Release Guide

This project ships portable release archives for testers and developers who should not need to build from source.

## Versioning

Version metadata is centralized in `Directory.Build.props`.

Current version fields:

- `Version`
- `AssemblyVersion`
- `FileVersion`
- `InformationalVersion`
- `Company`
- `Product`
- `RepositoryUrl`
- `Authors`
- `PackageLicenseExpression`
- `PackageProjectUrl`
- `RepositoryType`

Use semantic versioning:

- `MAJOR` for breaking changes.
- `MINOR` for backward-compatible features.
- `PATCH` for fixes and documentation-only release corrections.

`VERSION.txt` should match `Directory.Build.props` before a tagged release.

## Local Release Packaging

From the repository root:

```powershell
pwsh ./scripts/publish-all.ps1 -Version 1.0.3
```

This creates:

- `artifacts/releases/legacy-accessibility-static-crawler-1.0.3-win-x64.zip`
- `artifacts/releases/legacy-accessibility-static-crawler-1.0.3-linux-x64.tar.gz`
- `artifacts/releases/legacy-accessibility-static-crawler-1.0.3-osx-arm64.tar.gz`
- `artifacts/releases/legacy-accessibility-static-crawler-api-1.0.3-win-x64.zip`
- `artifacts/releases/legacy-accessibility-static-crawler-api-1.0.3-linux-x64.tar.gz`
- `artifacts/releases/legacy-accessibility-static-crawler-api-1.0.3-osx-arm64.tar.gz`
- `artifacts/releases/SHA256SUMS.txt`

Individual platform scripts are also available:

```powershell
pwsh ./scripts/publish-win-x64.ps1 -Version 1.0.3
```

```bash
./scripts/publish-linux-x64.sh 1.0.3
./scripts/publish-macos-arm64.sh 1.0.3
```

Each script restores, builds, tests, publishes a self-contained CLI executable, packages README/LICENSE/docs/samples, and writes the release archive under `artifacts/releases`.

API/UI package scripts are also available for the local browser-based workflow:

```powershell
pwsh ./scripts/publish-api-win-x64.ps1 -Version 1.0.3
```

```bash
./scripts/publish-api-linux-x64.sh 1.0.3
./scripts/publish-api-osx-arm64.sh 1.0.3
```

## GitHub Release Workflow

The release workflow runs when a version tag is pushed:

```bash
git tag v1.0.3
git push origin v1.0.3
```

It can also be started manually from GitHub Actions with a version input such as `1.0.3`.

The workflow:

1. Checks out the repository.
2. Installs .NET 8.
3. Restores dependencies.
4. Builds the solution in Release mode.
5. Runs tests.
6. Publishes Windows, Linux, and macOS ARM64 packages.
7. Uploads packages as workflow artifacts.
8. Creates or updates the GitHub Release.
9. Attaches packages and `SHA256SUMS.txt`.
10. Generates release notes from commits where GitHub supports it.

## Artifact Names

Release archives use this convention:

```text
legacy-accessibility-static-crawler-{version}-win-x64.zip
legacy-accessibility-static-crawler-{version}-linux-x64.tar.gz
legacy-accessibility-static-crawler-{version}-osx-arm64.tar.gz
legacy-accessibility-static-crawler-api-{version}-win-x64.zip
legacy-accessibility-static-crawler-api-{version}-linux-x64.tar.gz
legacy-accessibility-static-crawler-api-{version}-osx-arm64.tar.gz
SHA256SUMS.txt
```

`osx-arm64` is the .NET runtime identifier for Apple Silicon macOS.

## Verifying Package Integrity

Download the archive and `SHA256SUMS.txt` from the same GitHub Release.

Linux/macOS:

```bash
shasum -a 256 -c SHA256SUMS.txt
```

If a self-contained executable is run from a locked-down or unusual mounted volume and cannot create its bundle extraction cache, set an explicit extraction directory:

```bash
export DOTNET_BUNDLE_EXTRACT_BASE_DIR=/tmp/legacy-a11y-crawler
```

Windows PowerShell:

```powershell
Get-FileHash .\legacy-accessibility-static-crawler-1.0.3-win-x64.zip -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

Confirm the computed hash matches the entry in `SHA256SUMS.txt`.

## Tester Download Instructions

1. Open the GitHub Releases page.
2. Download the archive for your operating system.
3. Download `SHA256SUMS.txt`.
4. Verify the checksum.
5. Extract the archive.
6. Run the CLI executable from the extracted folder.

## Rollback

To roll back a release:

1. Mark the bad GitHub Release as pre-release or delete the release artifact if necessary.
2. Publish a patch release with the fix, such as `v1.0.4`.
3. Document the reason in `CHANGELOG.md`.
4. Ask testers to replace the extracted folder with the corrected release package.

Avoid reusing version tags after a package has been distributed.

## Distribution Model

Release ZIP/tar archives are the preferred distribution model. The CLI depends on browser automation and external browser/driver compatibility, so a global .NET tool package is not enabled initially. Portable archives make it clearer which docs, samples, driver binaries, and executable were approved together for a given release.
