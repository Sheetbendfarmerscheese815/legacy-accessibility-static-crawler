# Dependencies and Packaging

This document explains what a user needs to install, what is bundled in release packages, and how to distribute the tool without asking users to clone source code.

## Short Answer

For most users, provide a portable ZIP release.

They should not need to install the .NET SDK or build the source code when using a self-contained release package.

They do still need:

- An approved test workstation.
- Microsoft Edge or Google Chrome installed.
- Permission to crawl the target website.
- Permission to store generated screenshots, HTML captures, and reports.

## What Gets Packaged

Portable release ZIPs should include:

- Executable files.
- `appsettings.example.json`.
- `README.md`.
- `docs/`.
- `samples/`.
- `LICENSE`.
- `VERSION.txt`.
- `CHANGELOG.md`.

Release ZIPs must not include:

- `reports/`.
- `screenshots/`.
- raw HTML captures.
- `.env`.
- credentials.
- cookies.
- browser profile data.
- user-specific output.

## Package Types

There are two useful package types.

| Package | Audience | What It Does |
| --- | --- | --- |
| CLI package | Technical users, automation, CI, scripted scans | Runs `legacy-a11y-crawler` commands. |
| API/UI package | Non-technical users, local desktop workflow | Runs a local web app with `/ui/` and `/swagger`. |

The API/UI package is usually friendlier for non-technical users because it provides a form and report dashboard.

## Browser and Driver Dependencies

The project references Selenium WebDriver packages:

- `Selenium.WebDriver`
- `Selenium.WebDriver.ChromeDriver`
- `Selenium.WebDriver.MSEdgeDriver`

These packages can copy WebDriver executables into build or publish output for supported platforms.

However, WebDriver binaries are not the same thing as the browser.

Users still need the real browser installed:

- Google Chrome for `chrome`.
- Microsoft Edge for `modern-edge`.
- Microsoft Edge with IE mode configured by the organization for `edge-ie-mode-assisted`.

## Browser Driver Compatibility Matrix

| Browser Mode | Browser Required | Driver Package Pin | Compatibility Note |
| --- | --- | --- | --- |
| `chrome` | Google Chrome | `Selenium.WebDriver.ChromeDriver` `131.0.6778.20400` | Best with Chrome major version `131`. If enterprise Chrome is newer or older, IT may need to approve a matching ChromeDriver package. |
| `modern-edge` | Microsoft Edge | `Selenium.WebDriver.MSEdgeDriver` `147.0.3912.98` | Best with Edge major version `147`. Match EdgeDriver major version to installed Edge major version. |
| `edge-ie-mode-assisted` | Microsoft Edge with organization-managed IE mode | `Selenium.WebDriver.MSEdgeDriver` `147.0.3912.98` | Driver compatibility is necessary but not sufficient. IE mode also depends on Edge policy/site-list configuration and still requires manual validation. |

Driver binaries execute native code. For higher-assurance environments, keep the NuGet lockfile or package cache used for release builds, record package hashes during release approval, and let IT replace driver binaries only with approved versions that match the installed browsers.

## Can We Bundle Browser Drivers?

Yes, browser driver binaries can be included in the publish output via NuGet packages, and the current project is set up that way.

Important details:

- ChromeDriver and EdgeDriver must be compatible with installed browser versions.
- Enterprise desktops may pin browser versions, so driver version alignment matters.
- Some organizations require WebDriver binaries to be approved by IT/security.
- IE mode requires Edge and enterprise IE-mode configuration; a driver alone cannot provide IE-mode compatibility.

For enterprise distribution, the best practice is:

1. Ship the portable ZIP with the driver binaries produced by publish.
2. Document the browser versions tested with the release.
3. Allow IT teams to replace or approve browser drivers if their browser version differs.

## What Users Need To Download

### Portable Release Users

Users download only:

- The release ZIP for their operating system.

They also need Chrome or Edge already installed on the workstation.

### Source Build Users

Developers building from source need:

- .NET 8 SDK.
- Git.
- Chrome or Edge.
- Network access to NuGet during restore.

## Windows User Workflow

For a CLI ZIP:

```powershell
legacy-a11y-crawler.exe --help
legacy-a11y-crawler.exe version
legacy-a11y-crawler.exe crawl ^
  --url https://example.gov ^
  --browser modern-edge ^
  --max-pages 10 ^
  --depth 1 ^
  --output reports/example
```

For an API/UI ZIP:

```powershell
LegacyAccessibilityCrawler.Api.exe --urls http://localhost:5055
```

Then open:

```text
http://localhost:5055/ui/
```

## macOS User Workflow

For CLI:

```bash
./legacy-a11y-crawler --help
./legacy-a11y-crawler version
./legacy-a11y-crawler crawl \
  --url https://example.gov \
  --browser chrome \
  --max-pages 10 \
  --depth 1 \
  --output reports/example
```

For API/UI:

```bash
./LegacyAccessibilityCrawler.Api --urls http://localhost:5055
```

Then open:

```text
http://localhost:5055/ui/
```

macOS may require approving the executable in System Settings if the ZIP is not notarized.

## Linux User Workflow

For CLI:

```bash
./legacy-a11y-crawler --help
./legacy-a11y-crawler version
./legacy-a11y-crawler crawl \
  --url https://example.gov \
  --browser chrome \
  --max-pages 10 \
  --depth 1 \
  --output reports/example
```

For API/UI:

```bash
./LegacyAccessibilityCrawler.Api --urls http://localhost:5055
```

Then open:

```text
http://localhost:5055/ui/
```

Linux workstations may require Chrome or Edge dependencies installed by the desktop image.

## Creating Portable Packages

From the repository root:

```bash
dotnet restore legacy-accessibility-static-crawler.sln
dotnet build legacy-accessibility-static-crawler.sln -c Release --no-restore
```

Publish CLI packages:

```powershell
./scripts/publish-win-x64.ps1 -Version 1.0.0
```

```bash
./scripts/publish-linux-x64.sh 1.0.0
./scripts/publish-osx-arm64.sh 1.0.0
```

Publish API/UI packages:

```bash
./scripts/publish-api-linux-x64.sh 1.0.0
./scripts/publish-api-osx-arm64.sh 1.0.0
```

Windows API/UI publishing is included in:

```powershell
./scripts/publish-win-x64.ps1 -Version 1.0.0
```

Create ZIP packages:

```powershell
./scripts/create-release-package.ps1 -Version 1.0.0
```

## GitHub Releases

The GitHub Actions release workflow builds and uploads ZIP packages when a version tag is pushed:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow:

- Restores packages.
- Builds the solution.
- Runs tests.
- Publishes CLI artifacts for Windows, Linux, and macOS.
- Publishes API/UI artifacts.
- Creates ZIP packages.
- Uploads artifacts to GitHub Releases.

## MSI Installer

An MSI installer is possible, but it is intentionally not the first packaging option.

Why ZIP first:

- Easier to review and approve.
- Works well on locked-down test workstations.
- Avoids installer permissions.
- Avoids writing system-wide state.
- Easier to delete after an assessment.

Recommended future MSI approach:

- Use WiX Toolset.
- Install the API/UI package under `Program Files`.
- Add Start Menu shortcut for the UI host.
- Optionally install a Windows service only if enterprise users ask for always-on hosting.
- Keep reports in a user-selected folder, not inside the install directory.

## Recommended First Release

For non-technical users, publish:

- `legacy-a11y-api-win-x64-1.0.0.zip`
- `legacy-a11y-api-osx-arm64-1.0.0.zip`
- `legacy-a11y-api-linux-x64-1.0.0.zip`

For technical users and automation, publish:

- `legacy-a11y-crawler-win-x64-1.0.0.zip`
- `legacy-a11y-crawler-osx-arm64-1.0.0.zip`
- `legacy-a11y-crawler-linux-x64-1.0.0.zip`

## Honest Limitations

Portable packages make the tool easier to run, but they do not remove accessibility testing limitations.

The tool still does not:

- Certify compliance.
- Replace manual testing.
- Guarantee full coverage.
- Bypass authentication.
- Store credentials.
- Fully automate IE-mode systems.
