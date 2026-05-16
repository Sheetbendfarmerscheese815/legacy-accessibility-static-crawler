# UI Mode

The API project includes a lightweight browser UI for users who prefer forms and links instead of command-line commands.

## Start the UI

From the project folder:

```bash
cd /Volumes/HappyFam/genai-projects/legacy-accessibility-static-crawler
DOTNET_ROLL_FORWARD=Major dotnet run --project src/LegacyAccessibilityCrawler.Api
```

Open:

```text
http://localhost:5000/ui/
```

Swagger remains available at:

```text
http://localhost:5000/swagger
```

## What the UI Provides

The UI has two main areas:

- `Start a Crawl`: enter a website URL and choose crawl options from dropdowns and checkboxes.
- `Generated Reports`: view report folders and open generated files.

## Crawl Form Fields

| Field | What It Means |
| --- | --- |
| Website URL | The first page to crawl. |
| Browser mode | `Chrome`, `Modern Edge`, or `Edge IE-mode-assisted`. |
| Rule pack | WCAG 2.1 AA, optional WCAG 2.2, and optional Section 508 mapping. |
| Max pages | Maximum number of pages to capture. |
| Crawl depth | How far to follow links from the start page. |
| Delay between pages | Pause between page visits to reduce load on the site. |
| Output folder | Where reports are written. If blank, the UI creates a timestamped folder under `reports/ui-runs`. |
| Optional PDF rules path | Local path to an agency/internal rules PDF. |
| Run headless | Runs browser automation without showing a browser window. |
| Manual login/session required | Opens a visible browser session so a person can log in before crawling continues. |
| Stay on the same domain | Prevents the crawler from leaving the starting site by default. |
| Capture screenshots | Saves page screenshots as evidence. |
| Capture HTML evidence | Saves raw HTML as evidence. |

## Headless Crawling

For normal public sites, leave `Run headless` checked. The crawler will launch browser automation in the background and generate reports when the job completes.

Headless mode is automatically disabled when:

- Manual login/session is selected.
- Edge IE-mode-assisted is selected.

This is intentional because login and IE-mode workflows require a visible browser and human confirmation.

## Authenticated Websites

For authenticated sites:

1. Enter the site URL.
2. Choose `Chrome` or `Modern Edge`.
3. Check `Manual login/session required`.
4. Start the crawl.
5. Log in manually when the browser opens.
6. Return to the terminal and press Enter when prompted.

Credentials are not stored by the tool.

## Edge IE-Mode-Assisted Sites

Choose `Edge IE-mode-assisted` only for legacy systems that may require IE mode.

Important limitations:

- IE-mode automation is assisted, not complete.
- Some controls may not expose full DOM evidence.
- ActiveX, object/embed/applet, frames, and old document modes require manual review.
- Keyboard and screen reader validation remain required.

Reports will include IE-mode manual review guidance when applicable.

## Dashboard and Report Links

The dashboard reads generated report folders under `reports/`.

For each report, it links to:

- HTML report
- Azure DevOps CSV
- Findings CSV
- Markdown report
- JSON report
- Executive summary

The Azure DevOps file is named:

```text
ado-items.csv
```

## Suggested Non-Technical Workflow

1. Open `http://localhost:5000/ui/`.
2. Enter the website URL.
3. Choose `Chrome`.
4. Keep `Run headless` checked for public sites.
5. Use `Max pages = 5` and `Crawl depth = 1` for the first run.
6. Start the crawl.
7. Wait for the status box to say the crawl completed.
8. Use the dashboard to open the HTML report.
9. Download or open `ADO CSV` for Azure DevOps backlog import.

## Security Notes

Generated reports may contain sensitive content from pages, screenshots, or raw HTML.

Do not commit or casually share:

- `reports/`
- `screenshots/`
- `raw-html/`
- cookies
- browser profile data
- credentials

Use only on authorized systems.
