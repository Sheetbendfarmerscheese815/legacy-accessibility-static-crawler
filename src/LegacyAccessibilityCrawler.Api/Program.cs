using System.Collections.Concurrent;
using LegacyAccessibilityCrawler.Core;
using LegacyAccessibilityCrawler.Infrastructure;
using LegacyAccessibilityCrawler.Reporting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

app.MapGet("/api/version", () => new
{
    name = ProductInfo.Name,
    version = ProductInfo.Version,
    commit = ProductInfo.Commit,
    buildDateUtc = ProductInfo.BuildDateUtc,
    runtime = ".NET 8",
    disclaimer = ComplianceDisclaimers.Standard
});

app.MapPost("/api/crawl/start", async (CrawlerOptions options, JobStore jobs, IServiceProvider services, CancellationToken cancellationToken) =>
{
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
            if (options.EnablePdfRuleOverlay && !string.IsNullOrWhiteSpace(options.RulesPdfPath))
            {
                var extracted = await pdf.ExtractAsync(options.RulesPdfPath, Path.Combine(options.OutputDirectory, "rules"), cancellationToken);
                pdfRules.AddRange(extracted.Rules);
            }

            var rules = await rulePack.LoadRulesAsync(options.EnableSection508Rules, options.EnableWcag22EnhancedRules, pdfRules, cancellationToken);
            var pages = await crawler.CrawlAsync(options, cancellationToken);
            var findings = matcher.EnrichFindings(pages.SelectMany(p => engine.Analyze(p, rules)), rules);
            var result = new ScanResult
            {
                Options = options,
                Pages = pages,
                Findings = findings,
                RulesUsed = rules,
                Disclaimer = options.BrowserMode == BrowserMode.EdgeIeModeAssisted ? $"{ComplianceDisclaimers.Standard} {ComplianceDisclaimers.IeMode}" : ComplianceDisclaimers.Standard
            };
            var manifest = await reporting.GenerateAsync(result, options.OutputDirectory, cancellationToken);
            jobs.Complete(job.Id, new { result.ScanId, manifest });
        }
        catch (Exception ex)
        {
            jobs.Fail(job.Id, ex);
        }
    }, cancellationToken);
    return Results.Accepted($"/api/jobs/{job.Id}", job);
});

app.MapPost("/api/rules/extract", async (RulesExtractRequest request, IPdfRulesLoaderService pdf, CancellationToken cancellationToken) =>
    await pdf.ExtractAsync(request.RulesPdfPath, request.OutputDirectory, cancellationToken));

app.MapPost("/api/findings/import", async (ManualFindingsImportRequest request, IManualFindingsImporter importer, CancellationToken cancellationToken) =>
    await importer.ImportAsync(request.CsvPath, cancellationToken));

app.MapPost("/api/report/generate", async (ScanResult result, IReportGenerator generator, CancellationToken cancellationToken) =>
    await generator.GenerateAsync(result, result.Options.OutputDirectory, cancellationToken));

app.MapGet("/api/jobs/{id}", (string id, JobStore jobs) => jobs.TryGet(id, out var job) ? Results.Ok(job) : Results.NotFound());

app.MapGet("/api/reports/{id}", (string id) =>
{
    var report = Directory.EnumerateFiles("reports", "report.html", SearchOption.AllDirectories)
        .FirstOrDefault(p => p.Contains(id, StringComparison.OrdinalIgnoreCase));
    return report is null ? Results.NotFound() : Results.File(report, "text/html");
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
