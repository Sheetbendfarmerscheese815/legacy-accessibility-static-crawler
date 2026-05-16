# Deployment Notes

## Portable ZIP Deployment

Initial releases are ZIP-based portable packages, not MSI installers.

Each package contains the executable, `appsettings.example.json`, docs, samples, README, LICENSE, VERSION.txt, and CHANGELOG.md.

## Approved Test Workstations

Run scans from approved accessibility test workstations. Confirm the target owner has authorized crawling and evidence collection.

## Browser Driver Prerequisites

Selenium requires compatible Edge or Chrome browser binaries and matching drivers. The project references WebDriver packages, but enterprise images may require locally approved browser/driver installation.

## Edge and Chrome Compatibility

Keep browser versions and driver versions aligned. If WebDriver launch fails, check enterprise update rings, installed browser version, and endpoint restrictions.

## IE-mode Limitations

IE-mode support is assisted. It does not provide full legacy DOM automation. Manual keyboard, screen reader, and workflow validation are required.

## Report Locations

Reports are written to the configured output directory. Avoid writing reports to shared folders unless captures have been reviewed for sensitive content.

## Sensitive Captures

Disable screenshots or HTML capture for high-sensitivity systems when evidence storage is not approved. Do not commit reports, screenshots, cookies, raw HTML, or browser profile data.
