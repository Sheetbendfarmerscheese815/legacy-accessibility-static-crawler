# Scanning Modes and Accessibility Hooks

This document explains how to choose a scan mode, how legacy Struts-style applications are traversed, and what the Microsoft Axe hook does today.

The crawler is an accessibility assessment assistant. It collects evidence and creates remediation-oriented findings. It does not certify ADA, WCAG, or Section 508 compliance.

## Scan Modes

| Mode | Best For | What It Does |
| --- | --- | --- |
| `dynamic` | Modern pages, JavaScript-rendered content, pages that redirect after scripts run | Opens Chrome or Edge through Selenium, waits for the DOM to stabilize, captures page source, screenshots, and browser-derived evidence. |
| `static-stair` | Legacy server-rendered applications, especially Struts 1.x style `.do` routes and form-post navigation | Downloads HTML with an HTTP client, preserves session cookies, extracts anchors and form actions, and crawls level by level by depth. |
| `hybrid` | Mixed legacy and modern systems | Runs static-stair first, then dynamic browser capture, and de-duplicates pages by sanitized URL. |

For a first run on an unknown application, use `dynamic` with a small page limit. For Struts 1.x or traditional server-rendered applications, use `static-stair` or `hybrid`.

## Static Stair Mode

Static stair mode is a hierarchical crawler. It starts at the URL you provide, captures that page, extracts navigation targets, then moves outward one depth level at a time.

It extracts:

- Traditional anchor links: `<a href="...">`
- Form actions: `<form action="...">`
- Relative action paths such as `search.do`, `/account/details.do`, and `../workflow/submit.do`

This is useful for Struts 1.x applications because many screens are represented as server-side `.do` action mappings rather than modern client-side routes.

### Session Cookies

Static stair mode uses one HTTP client and one cookie container for the crawl. That allows normal session cookies set by the application to flow from one captured page to the next.

Important limits:

- It does not store cookies after the scan.
- It does not store usernames or passwords.
- It cannot complete interactive login screens by itself.
- If the application requires manual login, use `dynamic` or `hybrid` with manual session mode.

## Dynamic Mode

Dynamic mode uses Selenium and a real browser.

It waits for:

- Browser navigation
- `document.readyState`
- Short DOM size stabilization
- Optional configured delay

This helps with pages that use JavaScript redirects, asynchronous rendering, or dynamic DOM injection.

Dynamic mode is the better choice when content appears only after JavaScript runs.

If the local browser or compatible bundled driver cannot start, the default configuration falls back to `static-stair` mode. This keeps legacy assessments moving on locked-down workstations, but it cannot capture JavaScript-rendered content, screenshots, or browser-only evidence.

## Hybrid Mode

Hybrid mode combines both approaches:

1. Static stair crawl for server-rendered routes, `.do` actions, and form action discovery.
2. Dynamic browser crawl for rendered DOM, screenshots, and JavaScript-driven pages.
3. De-duplication by sanitized URL.

Use hybrid mode when a system has both legacy server-side navigation and dynamic widgets.

## Microsoft Axe Hook

The core code includes an `IAccessibilityEngine` abstraction and a `MicrosoftAxeAccessibilityEngine` hook.

The hook is intentionally disabled by default:

```json
{
  "Crawler": {
    "EnableMicrosoftAxe": false,
    "MicrosoftAxeRunnerPath": "",
    "MicrosoftAxeTimeoutSeconds": 30
  }
}
```

When `EnableMicrosoftAxe` is set to `true`, each captured page is routed through the accessibility engine interface after static checks run. This happens for pages captured by static stair, dynamic, and hybrid scans.

If `MicrosoftAxeRunnerPath` is empty, the tool records an informational manual-review finding that confirms the hook path was reached.

If `MicrosoftAxeRunnerPath` points to an approved local executable, the tool:

1. Writes the captured page HTML to a temporary local file.
2. Runs the configured executable with `--input <html-file> --url <page-url>`.
3. Reads axe-core style JSON from standard output.
4. Maps each violation into `AccessibilityFinding`.
5. Deletes the temporary HTML file.

The runner contract intentionally keeps the engine decoupled from this .NET solution. An organization can wrap Microsoft.Axe.Windows, axe-core, or another approved local engine as long as the wrapper emits axe-core compatible JSON.

A runner must:

- Runs the selected Axe engine locally.
- Does not upload page content to cloud services.
- Receives the captured DOM through the `--input` file.
- Emits deterministic axe-core compatible JSON to stdout.
- Keeps built-in WCAG/Section 508 static checks enabled.

## CLI Examples

Dynamic browser scan:

```bash
legacy-a11y-crawler crawl \
  --url https://example.gov \
  --scan-mode dynamic \
  --browser chrome \
  --max-pages 10 \
  --depth 1 \
  --output reports/example-dynamic
```

Static stair scan for a Struts-style application:

```bash
legacy-a11y-crawler crawl \
  --url https://legacy.example.gov/home.do \
  --scan-mode static-stair \
  --browser modern-edge \
  --max-pages 25 \
  --depth 2 \
  --output reports/legacy-static
```

Hybrid scan with the Microsoft Axe hook enabled:

```bash
legacy-a11y-crawler crawl \
  --url https://legacy.example.gov/home.do \
  --scan-mode hybrid \
  --browser modern-edge \
  --enable-microsoft-axe true \
  --microsoft-axe-runner ./tools/axe-runner \
  --max-pages 25 \
  --depth 2 \
  --output reports/legacy-hybrid
```

## UI Use

In UI mode:

1. Open `/ui/`.
2. Enter the website URL.
3. Choose `Scan mode`.
4. Choose the browser mode.
5. Start with a small page limit.
6. Review the dashboard links when the job completes.

For non-technical users, `Dynamic browser scan` is the safest default. Choose `Static stair scan` when the site is known to be a server-rendered legacy application. Choose `Hybrid` when both patterns are present.

## Packaging and Drivers

Release packages are self-contained .NET executables. Users do not need the .NET SDK.

The project references pinned Selenium driver packages and the release scripts copy the driver binaries into the package root. At runtime, the crawler resolves drivers locally beside the executable before using Selenium's default lookup.

The browser itself is still required:

- Chrome mode needs Google Chrome.
- Modern Edge mode needs Microsoft Edge.
- Edge IE-mode-assisted needs Microsoft Edge plus organization-managed IE-mode configuration.

This is the honest zero-dependency boundary: the app is packaged as a portable executable, but enterprise browsers and IE-mode policy remain workstation prerequisites.
