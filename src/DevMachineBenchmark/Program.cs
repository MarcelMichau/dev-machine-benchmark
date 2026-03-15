using DevMachineBenchmark.Benchmarks;
using DevMachineBenchmark.Benchmarks.Tasks;
using DevMachineBenchmark.Preflight;
using DevMachineBenchmark.Reporting;
using DevMachineBenchmark.SystemInfo;

var iterations = 5;
var warmupIterations = 1;
var skipDocker = false;
var shuffleSuites = false;
var preflightOnly = false;
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "results");
string? compareFileA = null;
string? compareFileB = null;
string? fromJsonFile = null;

// Parse CLI arguments
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--iterations" when i + 1 < args.Length:
            iterations = int.Parse(args[++i]);
            break;
        case "--warmup" when i + 1 < args.Length:
            warmupIterations = int.Parse(args[++i]);
            break;
        case "--skip-docker":
            skipDocker = true;
            break;
        case "--preflight-only":
            preflightOnly = true;
            break;
        case "--shuffle-suites":
            shuffleSuites = true;
            break;
        case "--output-dir" when i + 1 < args.Length:
            outputDir = args[++i];
            break;
        case "--compare" when i + 2 < args.Length:
            compareFileA = args[++i];
            compareFileB = args[++i];
            break;
        case "--from-json" when i + 1 < args.Length:
            fromJsonFile = args[++i];
            break;
        case "--help":
            PrintUsage();
            return;
        default:
            Console.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return;
    }
}

// Comparison mode
if (compareFileA is not null && compareFileB is not null)
{
    ComparisonReporter.Compare(compareFileA, compareFileB);
    return;
}

// Re-render markdown from existing JSON
if (fromJsonFile is not null)
{
    var existingReport = JsonReportWriter.Read(fromJsonFile);
    if (existingReport is null)
    {
        Console.WriteLine($"ERROR: Could not read {fromJsonFile}");
        return;
    }
    var dir = Path.GetDirectoryName(fromJsonFile) ?? ".";
    var renderedMdPath = await MarkdownReportWriter.WriteAsync(existingReport, dir);
    Console.WriteLine($"Markdown written to: {renderedMdPath}");
    return;
}

// Banner
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║              Dev Machine Benchmark Harness                       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine($"  Iterations: {iterations} (+ {warmupIterations} warm-up)");
if (shuffleSuites)
    Console.WriteLine("  Suite order: randomized");
Console.WriteLine();

// Preflight checks
var toolChecks = await PreflightValidator.ValidateAsync();
PreflightValidator.PrintResults(toolChecks);

if (!PreflightValidator.HasRequiredTools(toolChecks))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Aborting: required tools are missing.");
    Console.ResetColor();
    return;
}

if (preflightOnly)
    return;

var hasNode = PreflightValidator.HasTool(toolChecks, "node");
var hasNpm = PreflightValidator.HasTool(toolChecks, "npm");
var hasPnpm = PreflightValidator.HasTool(toolChecks, "pnpm");
var containerRuntime = PreflightValidator.GetContainerRuntime(toolChecks);
var hasDocker = containerRuntime is not null;
var runDockerPulls = hasDocker && !skipDocker;

// Collect system info
Console.WriteLine("Collecting system information...");
var machineInfo = await SystemInfoCollector.CollectAsync();

if (machineInfo.CpuUsagePercent is > 50)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"  Warning: CPU usage is {machineInfo.CpuUsagePercent:F1}% — results may be affected by background activity.");
    Console.ResetColor();
}

// Build suites
var suites = new List<BenchmarkSuite>();

// Suite 1: fake-survey-generator
{
    const string repoUrl = "https://github.com/MarcelMichau/fake-survey-generator.git";
    const string repoDir = "fake-survey-generator";
    const string solution = "FakeSurveyGenerator.slnx";

    var tasks = new List<IBenchmarkTask>
    {
        // NBGV requires full history to calculate version height — no shallow clone
        new GitCloneTask(repoUrl, repoDir, shallow: false),
        new DotNetRestoreTask(Path.Combine(repoDir, solution)),
        new DotNetBuildTask(Path.Combine(repoDir, solution)),
        new DotNetTestTask(Path.Combine(repoDir, "src/server/FakeSurveyGenerator.Application.Tests"), "unit"),
    };

    if (hasDocker)
    {
        tasks.Add(new DotNetTestTask(
            Path.Combine(repoDir, "src/server/FakeSurveyGenerator.Api.Tests.Integration"),
            "integration"));
        tasks.Add(new DotNetTestTask(
            Path.Combine(repoDir, "src/server/FakeSurveyGenerator.Acceptance.Tests"),
            "acceptance"));
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Skipping integration/acceptance tests for fake-survey-generator (Docker or Podman required)");
        Console.ResetColor();
    }

    if (hasNpm)
    {
        tasks.Add(new NpmInstallTask(Path.Combine(repoDir, "src/client/ui")));
        tasks.Add(new NpmBuildTask(Path.Combine(repoDir, "src/client/ui")));
        tasks.Add(new NpmTestTask(Path.Combine(repoDir, "src/client/ui")));
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Skipping npm tasks for fake-survey-generator (npm not found)");
        Console.ResetColor();
    }

    tasks.Add(new GitCommitTask(repoDir));

    suites.Add(new BenchmarkSuite("fake-survey-generator", repoUrl, tasks));
}

// Suite 2: dotnet-starter-project-template
{
    const string repoUrl = "https://github.com/MarcelMichau/dotnet-starter-project-template.git";
    const string repoDir = "dotnet-starter-project-template";
    const string solution = "DotNetStarterProjectTemplate.slnx";

    var tasks = new List<IBenchmarkTask>
    {
        new GitCloneTask(repoUrl, repoDir),
        new DotNetRestoreTask(Path.Combine(repoDir, solution)),
        new DotNetBuildTask(Path.Combine(repoDir, solution)),
    };

    if (hasDocker)
    {
        tasks.Add(new DotNetTestTask(
            Path.Combine(repoDir, "src/DotNetStarterProjectTemplate.AppHost.Tests"),
            "aspire"));
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Skipping Aspire tests for dotnet-starter-project-template (Docker or Podman required)");
        Console.ResetColor();
    }

    tasks.Add(new GitCommitTask(repoDir));

    suites.Add(new BenchmarkSuite("dotnet-starter-project-template", repoUrl, tasks));
}

// Suite 3: home-page
if (hasPnpm)
{
    const string repoUrl = "https://github.com/MarcelMichau/home-page.git";
    const string repoDir = "home-page";

    var tasks = new List<IBenchmarkTask>
    {
        new GitCloneTask(repoUrl, repoDir),
        new PnpmInstallTask(repoDir),
        new PnpmBuildTask(repoDir),
        new PlaywrightInstallTask(repoDir, usePnpm: true),
        new PlaywrightTestTask(repoDir, usePnpm: true),
        new GitCommitTask(repoDir),
    };

    suites.Add(new BenchmarkSuite("home-page", repoUrl, tasks));
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Skipping home-page suite (pnpm not found)");
    Console.ResetColor();
}

// Suite 4: Container image pulls (docker or podman)
if (runDockerPulls)
{
    var images = new[]
    {
        "mcr.microsoft.com/dotnet/sdk:10.0",
        "mcr.microsoft.com/dotnet/aspnet:10.0",
        "node:22-alpine",
        "postgres:17",
        "redis:7-alpine",
    };

    var tasks = images.Select(img => (IBenchmarkTask)new DockerPullTask(img, containerRuntime!)).ToList();
    suites.Add(new BenchmarkSuite("Container Image Pulls", null, tasks));
}
else if (!hasDocker)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Skipping container image pull suite (neither Docker nor Podman found)");
    Console.ResetColor();
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Skipping container image pull suite (--skip-docker)");
    Console.ResetColor();
}

// Shuffle suite order if requested to eliminate ordering bias
if (shuffleSuites)
{
    var rng = Random.Shared;
    for (var i = suites.Count - 1; i > 0; i--)
    {
        var j = rng.Next(i + 1);
        (suites[i], suites[j]) = (suites[j], suites[i]);
    }

    Console.WriteLine("  Suite execution order:");
    for (var i = 0; i < suites.Count; i++)
        Console.WriteLine($"    {i + 1}. {suites[i].Name}");
    Console.WriteLine();
}

// Run benchmarks
var runner = new BenchmarkRunner(iterations, warmupIterations, shuffleSuites);
var suiteResults = new List<SuiteResult>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nCancellation requested...");
};

foreach (var suite in suites)
{
    if (cts.Token.IsCancellationRequested) break;

    try
    {
        var result = await runner.RunSequentialSuiteAsync(suite, cts.Token);
        suiteResults.Add(result);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Benchmark cancelled.");
        break;
    }
}

// Build report
var report = new BenchmarkReport(
    machineInfo,
    DateTime.UtcNow,
    iterations,
    suiteResults,
    "Clone and Docker pull times are network-dependent. Git commit times are heavily impacted by DLP/endpoint protection.",
    warmupIterations);

// Output results
ConsoleReportWriter.Write(report);

var jsonPath = await JsonReportWriter.WriteAsync(report, outputDir);
var mdPath = await MarkdownReportWriter.WriteAsync(report, outputDir);
Console.WriteLine($"Results saved to: {jsonPath}");
Console.WriteLine($"              and {mdPath}");

static void PrintUsage()
{
    Console.WriteLine("""
        Usage: DevMachineBenchmark [options]

        Options:
          --iterations N           Measured iterations per task (default: 5)
          --warmup N               Warm-up iterations to discard (default: 1)
          --preflight-only         Run preflight checks and exit
          --skip-docker            Skip Docker pull benchmarks
          --shuffle-suites         Randomize suite execution order
          --output-dir PATH        Results directory (default: ./results)
          --compare A.json B.json  Compare two result files
          --from-json FILE         Re-render markdown from an existing JSON result
          --help                   Show this help
        """);
}
