# Security Policy

## Supported Version

Security fixes are accepted for the current `1.x` release line.

## Reporting a Vulnerability

Do not open a public issue with sensitive details. Report suspected vulnerabilities privately to the repository owner through GitHub security advisories or a private contact channel.

Please include:

- Affected version or commit.
- Steps to reproduce.
- Expected and actual behavior.
- Whether sensitive page content, credentials, cookies, or internal URLs were exposed.

## Sensitive Data Handling

This tool can capture HTML, screenshots, URLs, form structure, and report artifacts from authorized systems. Treat generated reports as assessment evidence that may contain sensitive information.

Do not commit:

- `.env`
- real `appsettings.json` secrets
- reports
- screenshots
- raw HTML captures
- cookies
- browser profile data
- storage state

## API Security Defaults

API routes under `/api/*` require an `x-api-key` header. Configure keys through `ApiSecurity:ApiKeys` or environment variables. Do not expose the API on a shared network without configuring allowed crawl domains and changing the sample local key.
