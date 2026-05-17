# Contributing

Thank you for helping improve `legacy-accessibility-static-crawler`.

## Development Workflow

1. Create a branch from `main`.
2. Keep changes focused on one fix or feature.
3. Run restore, build, and tests before opening a pull request.
4. Include documentation updates when behavior changes.
5. Do not commit generated reports, screenshots, raw HTML captures, cookies, or local secrets.

```bash
dotnet restore legacy-accessibility-static-crawler.sln
dotnet build legacy-accessibility-static-crawler.sln -c Release --no-restore
dotnet test legacy-accessibility-static-crawler.sln -c Release --no-build
```

## Pull Requests

Pull requests should describe:

- What changed.
- Why the change is needed.
- How it was tested.
- Any accessibility, security, or privacy impact.

## Code Style

- Use C# nullable annotations intentionally.
- Keep Core free of Infrastructure dependencies.
- Prefer deterministic static checks over subjective pass/fail claims.
- Mark human-judgment accessibility checks as manual review required.
- Keep compliance wording technically honest: this tool assists assessment; it does not certify compliance.
