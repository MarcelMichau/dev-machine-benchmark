using System.Diagnostics;
using System.Runtime.InteropServices;
using DevMachineBenchmark.Statistics;

namespace DevMachineBenchmark.Benchmarks;

public sealed class BenchmarkRunner(int iterations, int warmupIterations = 1, bool shuffleSuites = false)
{
    public async Task<SuiteResult> RunSuiteAsync(BenchmarkSuite suite, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== Suite: {suite.Name} ===");
        Console.ResetColor();

        var sw = Stopwatch.StartNew();
        var results = new List<BenchmarkResult>();

        foreach (var task in suite.Tasks)
        {
            Console.WriteLine();
            Console.WriteLine($"  Task: {task.Name} [{task.Category}]");

            var durations = new List<double>();
            var success = true;

            // Warm-up runs (discarded)
            for (var w = 1; w <= warmupIterations; w++)
            {
                Console.Write($"    [warm-up {w}/{warmupIterations}] ");
                var tempDir = CreateTempDirectory();
                try
                {
                    var warmup = await task.ExecuteAsync(tempDir, ct);
                    Console.WriteLine(warmup.Success
                        ? $"{warmup.Elapsed.TotalMilliseconds:F0}ms (discarded)"
                        : $"FAILED: {warmup.ErrorMessage}");

                    if (!warmup.Success)
                    {
                        success = false;
                        results.Add(new BenchmarkResult(task.Name, [], null, false, task.Category));
                        break;
                    }
                }
                finally
                {
                    await CleanupDirectoryAsync(tempDir);
                }
            }

            if (!success) continue;

            // Measured iterations
            for (var i = 1; i <= iterations; i++)
            {
                Console.Write($"    [iteration {i}/{iterations}] ");
                var tempDir = CreateTempDirectory();
                try
                {
                    var result = await task.ExecuteAsync(tempDir, ct);

                    if (result.Success)
                    {
                        durations.Add(result.Elapsed.TotalMilliseconds);
                        Console.WriteLine($"{result.Elapsed.TotalMilliseconds:F0}ms");
                    }
                    else
                    {
                        Console.WriteLine($"FAILED: {result.ErrorMessage}");
                        success = false;
                        break;
                    }
                }
                finally
                {
                    await CleanupDirectoryAsync(tempDir);
                }
            }

            var stats = ComputeStats(durations);
            results.Add(new BenchmarkResult(task.Name, durations, stats, success, task.Category));
        }

        return new SuiteResult(suite.Name, results, sw.Elapsed);
    }

    /// <summary>
    /// Runs a suite where all tasks share the same working directory per iteration
    /// (e.g., clone → restore → build → test → commit in sequence).
    /// </summary>
    public async Task<SuiteResult> RunSequentialSuiteAsync(BenchmarkSuite suite, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== Suite: {suite.Name} ===");
        Console.ResetColor();

        var sw = Stopwatch.StartNew();

        // We need to track per-task durations across iterations
        var taskDurations = new Dictionary<string, List<double>>();
        var taskSuccess = new Dictionary<string, bool>();
        var taskCategories = new Dictionary<string, TaskCategory>();
        foreach (var task in suite.Tasks)
        {
            taskDurations[task.Name] = [];
            taskSuccess[task.Name] = true;
            taskCategories[task.Name] = task.Category;
        }

        // Warm-up runs
        for (var w = 1; w <= warmupIterations; w++)
        {
            Console.WriteLine();
            Console.WriteLine($"  [warm-up iteration {w}/{warmupIterations}]");
            var warmupDir = CreateTempDirectory();
            try
            {
                foreach (var task in suite.Tasks)
                {
                    Console.Write($"    {task.Name}: ");
                    TaskResult result;
                    try
                    {
                        result = await task.ExecuteAsync(warmupDir, ct);
                    }
                    catch (Exception ex)
                    {
                        result = new TaskResult(TimeSpan.Zero, false, ex.Message);
                    }

                    Console.WriteLine(result.Success
                        ? $"{result.Elapsed.TotalMilliseconds:F0}ms (discarded)"
                        : $"FAILED: {result.ErrorMessage}");

                    if (!result.Success)
                    {
                        taskSuccess[task.Name] = false;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"    WARNING: {task.Name} failed during warm-up, will skip in measured runs");
                        Console.ResetColor();
                    }
                }
            }
            finally
            {
                await CleanupDirectoryAsync(warmupDir);
            }
        }

        // Measured iterations
        for (var i = 1; i <= iterations; i++)
        {
            Console.WriteLine();
            Console.WriteLine($"  [iteration {i}/{iterations}]");
            var iterDir = CreateTempDirectory();
            try
            {
                foreach (var task in suite.Tasks)
                {
                    if (!taskSuccess[task.Name])
                    {
                        Console.WriteLine($"    {task.Name}: SKIPPED (failed in warm-up)");
                        continue;
                    }

                    Console.Write($"    {task.Name}: ");
                    TaskResult result;
                    try
                    {
                        result = await task.ExecuteAsync(iterDir, ct);
                    }
                    catch (Exception ex)
                    {
                        result = new TaskResult(TimeSpan.Zero, false, ex.Message);
                    }

                    if (result.Success)
                    {
                        taskDurations[task.Name].Add(result.Elapsed.TotalMilliseconds);
                        Console.WriteLine($"{result.Elapsed.TotalMilliseconds:F0}ms");
                    }
                    else
                    {
                        Console.WriteLine($"FAILED: {result.ErrorMessage}");
                        taskSuccess[task.Name] = false;
                    }
                }
            }
            finally
            {
                await CleanupDirectoryAsync(iterDir);
            }
        }

        // Build results
        var results = new List<BenchmarkResult>();
        foreach (var task in suite.Tasks)
        {
            var durations = taskDurations[task.Name];
            var stats = ComputeStats(durations);
            results.Add(new BenchmarkResult(task.Name, durations, stats, taskSuccess[task.Name], taskCategories[task.Name]));
        }

        return new SuiteResult(suite.Name, results, sw.Elapsed);
    }

    public bool ShuffleSuites => shuffleSuites;

    private static TaskStats? ComputeStats(List<double> durations)
    {
        if (durations.Count == 0) return null;

        var arr = durations.ToArray();
        var cv = StatsCalculator.CoefficientOfVariation(arr);
        var ci = StatsCalculator.ConfidenceInterval95(arr);
        var iqr = arr.Length >= 4 ? StatsCalculator.IQR(arr) : (double?)null;
        var trimmedMean = arr.Length >= 5 ? StatsCalculator.TrimmedMean(arr) : (double?)null;
        var hasOutliers = StatsCalculator.HasOutliers(arr);
        var outlierCount = StatsCalculator.OutlierCount(arr);

        return new TaskStats(
            StatsCalculator.Mean(arr),
            StatsCalculator.Median(arr),
            StatsCalculator.StdDev(arr),
            StatsCalculator.Min(arr),
            StatsCalculator.Max(arr),
            cv,
            ci?.Low,
            ci?.High,
            iqr,
            trimmedMean,
            hasOutliers,
            outlierCount);
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dev-bench-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task CleanupDirectoryAsync(string path)
    {
        if (!Directory.Exists(path)) return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Handle long paths on Windows
                await ProcessRunner.RunAsync("cmd", $"/c rmdir /s /q \"{path}\"", ".");
            }
            else
            {
                await ProcessRunner.RunAsync("rm", $"-rf \"{path}\"", ".");
            }
        }
        catch
        {
            // Best effort cleanup
            try { Directory.Delete(path, true); } catch { }
        }
    }
}
