# Non-Technical User Guide

This guide is for users who need to run an accessibility assessment crawl, collect evidence, and create report files without needing to understand the source code.

## What This Tool Is

`legacy-accessibility-static-crawler` is an accessibility assessment assistant. It helps find common accessibility issues and creates reports that can support remediation planning.

It does not certify ADA, WCAG, or Section 508 compliance. Automated scanning is only one part of accessibility testing. Manual keyboard testing, screen reader testing, user-flow testing, and assistive technology testing are still required.

## Before You Start

Confirm these items before running a crawl:

- You are authorized to test the website.
- You know whether the site is public, behind login, or a legacy IE-mode system.
- You have permission to save screenshots and HTML captures.
- You know where reports should be stored.
- You understand that reports may contain sensitive page content.

Do not use this tool to crawl websites you do not own or do not have permission to test.

## Recommended Local Setup

Open a terminal and move into the project folder:

```bash
cd /Volumes/HappyFam/genai-projects/legacy-accessibility-static-crawler
```

On this workstation, the installed runtime may be newer than .NET 8. If commands fail with a missing .NET 8 runtime message, use:

```bash
DOTNET_ROLL_FORWARD=Major
```

In examples below, commands include `DOTNET_ROLL_FORWARD=Major` for compatibility.

## Easiest Option: Use the Browser UI

Start the local API and UI:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Api
```

Open:

```text
http://localhost:5000/ui/
```

The UI lets you enter the website URL, choose browser mode from a dropdown, set page limits, choose rule packs, and start a headless crawl. It also has a dashboard that links to generated reports and the Azure DevOps CSV.

For full UI instructions, see `docs/ui-mode.md`.

## Quick Health Check

Run these commands to confirm the tool is ready:

```bash
dotnet restore legacy-accessibility-static-crawler.sln
dotnet build legacy-accessibility-static-crawler.sln -c Release --no-restore
DOTNET_ROLL_FORWARD=Major dotnet test legacy-accessibility-static-crawler.sln -c Release --no-build
```

Expected result:

- Restore completes successfully.
- Build completes successfully.
- Tests pass, with one PDF extraction test skipped unless a real PDF fixture is supplied.

## Browser Choices

Use one of these browser modes:

| Browser Mode | When To Use It |
| --- | --- |
| `chrome` | Most modern websites when Chrome is available. |
| `modern-edge` | Modern websites when Microsoft Edge is preferred. |
| `edge-ie-mode-assisted` | Legacy systems that may require Microsoft Edge IE mode. This mode requires extra manual review. |

For the first local test, use `chrome` against a simple public site.

## Simple Public Website Crawl

This checks one public page and writes reports to `reports/example`.

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Cli -- crawl \
  --url https://example.com \
  --browser chrome \
  --max-pages 1 \
  --depth 0 \
  --output reports/example
```

After it completes, open the HTML report:

```bash
open reports/example/report.html
```

Generated files include:

| File | Purpose |
| --- | --- |
| `report.html` | Human-readable report for review. |
| `report.md` | Markdown version of the report. |
| `report.json` | Structured report data. |
| `findings.csv` | Spreadsheet-friendly list of findings. |
| `executive-summary.md` | Short summary for stakeholders. |
| `ado-items.csv` | Azure DevOps-ready backlog import file. |
| `scan-results.json` | Raw scan result used to regenerate reports later. |

## Crawling Authenticated Websites

For sites behind a login, use manual session mode. The tool opens a browser and waits while you sign in. It does not store your username, password, cookies, or browser profile by default.

Run:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Cli -- crawl \
  --url https://your-authenticated-site.example.gov \
  --browser chrome \
  --manual-session true \
  --max-pages 10 \
  --depth 1 \
  --output reports/authenticated-site
```

What happens next:

1. A browser window opens.
2. Log in manually using your normal approved process.
3. Complete any multi-factor authentication yourself.
4. Return to the terminal.
5. Press Enter when the tool asks you to continue.
6. The crawler captures pages using the authenticated browser session.

Use a small `--max-pages` value for the first run. Increase it only after confirming the first report looks correct.

## Authenticated Crawl With Microsoft Edge

Use this if your organization requires Edge:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Cli -- crawl \
  --url https://your-authenticated-site.example.gov \
  --browser modern-edge \
  --manual-session true \
  --max-pages 10 \
  --depth 1 \
  --output reports/authenticated-edge
```

## Legacy IE-Mode-Assisted Crawl

Use this only for legacy systems that may require Microsoft Edge IE mode:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Cli -- crawl \
  --url https://legacy.example.gov \
  --browser edge-ie-mode-assisted \
  --manual-session true \
  --max-pages 10 \
  --depth 1 \
  --output reports/legacy-ie
```

IE-mode-assisted scans have important limitations:

- The crawler may not inspect every legacy control.
- ActiveX, object, embed, applet, frames, and old document modes may not expose complete evidence.
- Keyboard behavior must be manually tested.
- Screen reader behavior must be manually tested.
- The report will include an IE-mode manual review disclaimer when legacy risks or limited DOM capture are detected.

Do not describe IE-mode-assisted output as a complete automated accessibility test.

## Using a PDF Rules Overlay

If your agency or team has a PDF with internal standards, test procedures, or remediation guidance, you can use it as an overlay.

Extract rules first:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Cli -- extract-rules \
  --rules-pdf ./samples/sample-rules.pdf \
  --output ./reports/rules
```

Then crawl with the PDF:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Cli -- crawl \
  --url https://example.com \
  --browser chrome \
  --rules-pdf ./samples/sample-rules.pdf \
  --max-pages 5 \
  --depth 1 \
  --output reports/with-pdf-rules
```

PDF rules are guidance overlays. They do not replace the built-in WCAG 2.1 AA and Section 508 static rule packs.

## Getting the Azure DevOps CSV

The Azure DevOps CSV is created automatically whenever reports are generated.

After a crawl, look for:

```text
reports/YOUR-OUTPUT-FOLDER/ado-items.csv
```

Example:

```text
reports/authenticated-site/ado-items.csv
```

This CSV includes:

- Work Item Type
- Title
- Description
- Acceptance Criteria
- Severity
- Tags
- Source URL
- Rule Reference
- Evidence

Import this file into Azure DevOps using your organization’s normal CSV import process.

## Regenerating the ADO CSV Later

If you already have `scan-results.json`, regenerate reports and the ADO CSV with:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Cli -- report \
  --scan-results reports/authenticated-site/scan-results.json \
  --output reports/authenticated-site/final
```

The regenerated ADO CSV will be:

```text
reports/authenticated-site/final/ado-items.csv
```

## Using the Local API and Swagger

Start the API:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Api
```

Open Swagger:

```text
http://localhost:5000/swagger
```

Useful endpoints:

| Endpoint | Purpose |
| --- | --- |
| `GET /api/version` | Check tool version and build metadata. |
| `GET /api/rulepacks` | View loaded built-in rule packs. |
| `POST /api/rules/extract` | Extract PDF guidance rules. |
| `POST /api/crawl/start` | Start a crawl job. |
| `GET /api/jobs/{id}` | Check crawl job status. |
| `POST /api/findings/import` | Import manual findings from CSV. |
| `POST /api/report/generate` | Generate reports from scan data. |

For non-technical users, the CLI is usually simpler than Swagger for authenticated crawling because manual login requires a browser session and terminal confirmation.

## Manual Findings

Some issues cannot be proven by static scanning. Examples:

- Screen reader announcement quality.
- Keyboard traps.
- Whether focus is visibly obvious.
- Whether a form error is clear enough.
- Whether a workflow is understandable.
- Whether custom legacy controls work with assistive technology.

Use `samples/sample-manual-findings.csv` as a starting point for manual findings.

Manual findings are especially important for IE-mode-assisted systems.

## Choosing Safe Limits

Start small:

```text
--max-pages 5
--depth 1
```

Increase only after confirming:

- The crawler is staying inside the expected site.
- Reports are being written to the right folder.
- Screenshots and HTML captures are approved for storage.
- The target system can handle the crawl rate.

## Sensitive Data Guidance

Reports can contain page text, URLs, screenshots, and raw HTML. Treat reports as sensitive when testing internal or authenticated systems.

Recommended practices:

- Use a dedicated output folder for each assessment.
- Do not commit `reports/`, `screenshots/`, `raw-html/`, `cookies/`, or `storage-state/`.
- Avoid crawling production data unless approved.
- Redact or protect generated reports before sharing.
- Use manual login instead of storing credentials.

## What To Tell Stakeholders

Use this wording:

> This report identifies static accessibility findings and manual review areas. It supports remediation planning, but it does not certify ADA, WCAG, or Section 508 compliance. Manual keyboard, screen reader, assistive technology, and user-flow testing are still required.

Avoid saying:

- The site is compliant.
- The site passed accessibility testing.
- The crawler proved WCAG compliance.
- IE-mode controls were fully tested automatically.

## Troubleshooting

### The command says .NET 8 is missing

Use:

```bash
DOTNET_ROLL_FORWARD=Major
```

or install the .NET 8 SDK/runtime.

### The browser does not open

Confirm Chrome or Edge is installed and compatible with the WebDriver package. For enterprise machines, browser drivers may need to be approved by IT.

### The crawl logs in but finds too few pages

Try increasing:

```text
--max-pages
--depth
```

Also confirm links are normal links. Some JavaScript-only navigation may require manual review.

### The report contains sensitive information

Move the report to an approved secure location. Delete unauthorized captures. Re-run with a smaller scope or with screenshot/HTML capture disabled in configuration if needed.

### The ADO CSV is missing

Confirm the crawl finished successfully. If `scan-results.json` exists, regenerate reports with the `report` command.
