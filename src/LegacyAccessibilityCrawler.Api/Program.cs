using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using LegacyAccessibilityCrawler.Core;
using LegacyAccessibilityCrawler.Infrastructure;
using LegacyAccessibilityCrawler.Reporting;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API key required for /api routes. Use the x-api-key header.",
        Name = "x-api-key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
        }] = []
    });
});
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IRulePackService, RulePackService>();
builder.Services.AddSingleton<IStaticCheckEngine, StaticCheckEngine>();
builder.Services.AddSingleton<IRuleMatcherService, RuleMatcherService>();
builder.Services.AddSingleton<ICrawlerService, SeleniumCrawlerService>();
builder.Services.AddSingleton<IPdfRulesLoaderService, PdfRulesLoaderService>();
builder.Services.AddSingleton<IManualFindingsImporter, ManualFindingsImporter>();
builder.Services.AddSingleton<IReportGenerator, ReportGenerator>();
builder.Services.AddSingleton<ILlmReviewService, DisabledLlmReviewService>();
builder.Services.AddSingleton<JobStore>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    if (!ApiKeySecurity.IsApiRouteRequiringKey(context.Request.Path))
    {
        await next();
        return;
    }

    var configuredKeys = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("ApiSecurity:ApiKeys").Get<string[]>() ?? [];
    if (ApiKeySecurity.IsValid(context.Request.Headers["x-api-key"], configuredKeys))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    await context.Response.WriteAsJsonAsync(new { error = "A valid x-api-key header is required for API routes." });
});

app.MapGet("/", () => Results.Redirect("/ui/"));

app.MapGet("/api/version", () => new
{
    name = ProductInfo.Name,
    version = ProductInfo.Version,
    commit = ProductInfo.Commit,
    buildDateUtc = ProductInfo.BuildDateUtc,
    runtime = ".NET 8",
    disclaimer = ComplianceDisclaimers.Standard
});

app.MapPost("/api/crawl/start", async (CrawlerOptions options, JobStore jobs, IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    CrawlerOptions normalizedOptions;
    try
    {
        normalizedOptions = ApiRequestGuards.NormalizeCrawlerOptions(options, configuration);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    var job = jobs.Create("crawl");
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = services.CreateScope();
            var rulePack = scope.ServiceProvider.GetRequiredService<IRulePackService>();
            var crawler = scope.ServiceProvider.GetRequiredService<ICrawlerService>();
            var engine = scope.ServiceProvider.GetRequiredService<IStaticCheckEngine>();
            var matcher = scope.ServiceProvider.GetRequiredService<IRuleMatcherService>();
            var reporting = scope.ServiceProvider.GetRequiredService<IReportGenerator>();
            var pdf = scope.ServiceProvider.GetRequiredService<IPdfRulesLoaderService>();

            var pdfRules = new List<AccessibilityRule>();
            if (normalizedOptions.EnablePdfRuleOverlay && !string.IsNullOrWhiteSpace(normalizedOptions.RulesPdfPath))
            {
                var extracted = await pdf.ExtractAsync(normalizedOptions.RulesPdfPath, Path.Combine(normalizedOptions.OutputDirectory, "rules"), cancellationToken);
                pdfRules.AddRange(extracted.Rules);
            }

            var rules = await rulePack.LoadRulesAsync(normalizedOptions.EnableSection508Rules, normalizedOptions.EnableWcag22EnhancedRules, pdfRules, cancellationToken);
            var pages = await crawler.CrawlAsync(normalizedOptions, cancellationToken);
            var findings = matcher.EnrichFindings(pages.SelectMany(p => engine.Analyze(p, rules)), rules);
            var result = new ScanResult
            {
                Options = normalizedOptions,
                Pages = pages,
                Findings = findings,
                RulesUsed = rules,
                Disclaimer = normalizedOptions.BrowserMode == BrowserMode.EdgeIeModeAssisted ? $"{ComplianceDisclaimers.Standard} {ComplianceDisclaimers.IeMode}" : ComplianceDisclaimers.Standard
            };
            var manifest = await reporting.GenerateAsync(result, normalizedOptions.OutputDirectory, cancellationToken);
            jobs.Complete(job.Id, new { result.ScanId, manifest });
        }
        catch (Exception ex)
        {
            jobs.Fail(job.Id, ex);
        }
    }, cancellationToken);
    return Results.Accepted($"/api/jobs/{job.Id}", job);
});

app.MapPost("/api/rules/extract", async (RulesExtractRequest request, IPdfRulesLoaderService pdf, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    try
    {
        var output = ApiRequestGuards.ResolveOutputDirectory(request.OutputDirectory, configuration);
        return Results.Ok(await pdf.ExtractAsync(request.RulesPdfPath, output, cancellationToken));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/findings/import", async (ManualFindingsImportRequest request, IManualFindingsImporter importer, CancellationToken cancellationToken) =>
    await importer.ImportAsync(request.CsvPath, cancellationToken));

app.MapPost("/api/report/generate", async (ScanResult result, IReportGenerator generator, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    try
    {
        var output = ApiRequestGuards.ResolveOutputDirectory(result.Options.OutputDirectory, configuration);
        return Results.Ok(await generator.GenerateAsync(result with { Options = result.Options with { OutputDirectory = output } }, output, cancellationToken));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/jobs/{id}", (string id, JobStore jobs) => jobs.TryGet(id, out var job) ? Results.Ok(job) : Results.NotFound());

app.MapGet("/api/reports/{id}", (string id) =>
{
    var report = Directory.EnumerateFiles("reports", "report.html", SearchOption.AllDirectories)
        .FirstOrDefault(p => p.Contains(id, StringComparison.OrdinalIgnoreCase));
    return report is null ? Results.NotFound() : Results.File(report, "text/html");
});

app.MapGet("/api/reports", () =>
{
    var reportsRoot = Path.GetFullPath("reports");
    if (!Directory.Exists(reportsRoot))
    {
        return Results.Ok(Array.Empty<ReportListItem>());
    }

    var items = Directory.EnumerateFiles(reportsRoot, "report.html", SearchOption.AllDirectories)
        .Select(path => ReportListItem.FromReportPath(reportsRoot, path))
        .OrderByDescending(item => item.LastModifiedUtc)
        .ToList();

    return Results.Ok(items);
});

app.MapGet("/api/report-file", (string path) =>
{
    var reportsRoot = Path.GetFullPath("reports");
    var requested = Path.GetFullPath(Path.Combine(reportsRoot, path));
    var normalizedRoot = Path.TrimEndingDirectorySeparator(reportsRoot) + Path.DirectorySeparatorChar;
    var normalizedRequested = Path.TrimEndingDirectorySeparator(requested) + Path.DirectorySeparatorChar;
    if (!normalizedRequested.StartsWith(normalizedRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) || !File.Exists(requested))
    {
        return Results.NotFound();
    }

    var contentType = Path.GetExtension(requested).ToLowerInvariant() switch
    {
        ".html" => "text/html",
        ".md" => "text/markdown",
        ".json" => "application/json",
        ".csv" => "text/csv",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };

    return Results.File(requested, contentType, Path.GetFileName(requested));
});

app.MapGet("/api/rulepacks", async (IRulePackService rules, CancellationToken cancellationToken) =>
    await rules.LoadRulesAsync(includeSection508: true, includeWcag22: true, cancellationToken: cancellationToken));

app.MapGet("/api/rulepacks/{id}", async (string id, IRulePackService rules, CancellationToken cancellationToken) =>
{
    var all = await rules.LoadRulesAsync(includeSection508: true, includeWcag22: true, cancellationToken: cancellationToken);
    return Results.Ok(all.Where(r => r.Standard.Contains(id, StringComparison.OrdinalIgnoreCase) || r.RuleId.Contains(id, StringComparison.OrdinalIgnoreCase)));
});

app.Run();

public sealed record RulesExtractRequest(string RulesPdfPath, string OutputDirectory);
public sealed record ManualFindingsImportRequest(string CsvPath);
public sealed record JobStatus(string Id, string Type, string Status, DateTimeOffset CreatedAtUtc, object? Result = null, string? Error = null);
public sealed record ReportListItem(
    string Name,
    string Directory,
    DateTimeOffset LastModifiedUtc,
    string HtmlPath,
    string MarkdownPath,
    string JsonPath,
    string FindingsCsvPath,
    string AdoCsvPath,
    string ExecutiveSummaryPath)
{
    public static ReportListItem FromReportPath(string reportsRoot, string htmlPath)
    {
        var directory = Path.GetDirectoryName(htmlPath) ?? reportsRoot;
        var relativeDirectory = Path.GetRelativePath(reportsRoot, directory);
        static string Relative(string reportsRoot, string directory, string fileName) =>
            Path.GetRelativePath(reportsRoot, Path.Combine(directory, fileName));

        return new ReportListItem(
            string.IsNullOrWhiteSpace(relativeDirectory) || relativeDirectory == "." ? "reports" : relativeDirectory,
            relativeDirectory,
            File.GetLastWriteTimeUtc(htmlPath),
            Relative(reportsRoot, directory, "report.html"),
            Relative(reportsRoot, directory, "report.md"),
            Relative(reportsRoot, directory, "report.json"),
            Relative(reportsRoot, directory, "findings.csv"),
            Relative(reportsRoot, directory, "ado-items.csv"),
            Relative(reportsRoot, directory, "executive-summary.md"));
    }
}

public sealed class JobStore
{
    private readonly ConcurrentDictionary<string, JobStatus> _jobs = new();
    public JobStatus Create(string type)
    {
        var job = new JobStatus(Guid.NewGuid().ToString("N"), type, "Running", DateTimeOffset.UtcNow);
        _jobs[job.Id] = job;
        return job;
    }
    public void Complete(string id, object result) => _jobs[id] = _jobs[id] with { Status = "Completed", Result = result };
    public void Fail(string id, Exception exception) => _jobs[id] = _jobs[id] with { Status = "Failed", Error = exception.Message };
    public bool TryGet(string id, out JobStatus? job) => _jobs.TryGetValue(id, out job);
}

public static class ApiKeySecurity
{
    public static bool IsApiRouteRequiringKey(PathString path) =>
        path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    public static bool IsValid(string? suppliedKey, IEnumerable<string> configuredKeys)
    {
        if (string.IsNullOrWhiteSpace(suppliedKey))
        {
            return false;
        }

        return configuredKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Any(key => string.Equals(key.Trim(), suppliedKey, StringComparison.Ordinal));
    }
}

public static class ApiRequestGuards
{
    public static CrawlerOptions NormalizeCrawlerOptions(CrawlerOptions options, IConfiguration configuration)
    {
        var allowedDomains = configuration.GetSection("Crawler:AllowedDomains").Get<string[]>() ?? [];
        return CrawlRequestValidator.ValidateAndNormalize(options, allowedDomains, BaseOutputDirectory(configuration));
    }

    public static string ResolveOutputDirectory(string? requestedPath, IConfiguration configuration) =>
        OutputPathSanitizer.ResolveOutputDirectory(requestedPath, BaseOutputDirectory(configuration));

    private static string BaseOutputDirectory(IConfiguration configuration) =>
        configuration.GetValue<string>("Security:BaseOutputDirectory") ?? "reports";
}
