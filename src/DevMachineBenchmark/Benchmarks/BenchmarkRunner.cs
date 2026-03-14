using System.Runtime.InteropServices;
using DevMachineBenchmark.Statistics;

namespace DevMachineBenchmark.Benchmarks;

public sealed class BenchmarkRunner(int iterations)
{
    public async Task<SuiteResult> RunSuiteAsync(BenchmarkSuite suite, CancellationToken ct)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== Suite: {suite.Name} ===");
        Console.ResetColor();

        var results = new List<BenchmarkResult>();

        foreach (var task in suite.Tasks)
        {
            Console.WriteLine();
            Console.WriteLine($"  Task: {task.Name}");

            var durations = new List<double>();
            var success = true;

            // Warm-up run (discarded)
            Console.Write("    [warm-up] ");
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
                    results.Add(new BenchmarkResult(task.Name, [], null, false));
                    continue;
                }
            }
            finally
            {
                await CleanupDirectoryAsync(tempDir);
            }

            // Measured iterations
            for (var i = 1; i <= iterations; i++)
            {
                Console.Write($"    [iteration {i}/{iterations}] ");
                tempDir = CreateTempDirectory();
                try
                {
                    // For tasks that need prior state (build before test, etc.),
                    // they operate on the working directory passed in by the suite orchestrator.
                    // For standalone tasks, each gets a fresh temp dir.
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

            TaskStats? stats = null;
            if (durations.Count > 0)
            {
                var arr = durations.ToArray();
                stats = new TaskStats(
                    StatsCalculator.Mean(arr),
                    StatsCalculator.Median(arr),
                    StatsCalculator.StdDev(arr),
                    StatsCalculator.Min(arr),
                    StatsCalculator.Max(arr));
            }

            results.Add(new BenchmarkResult(task.Name, durations, stats, success));
        }

        return new SuiteResult(suite.Name, results);
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

        // We need to track per-task durations across iterations
        var taskDurations = new Dictionary<string, List<double>>();
        var taskSuccess = new Dictionary<string, bool>();
        foreach (var task in suite.Tasks)
        {
            taskDurations[task.Name] = [];
            taskSuccess[task.Name] = true;
        }

        // Warm-up run
        Console.WriteLine();
        Console.WriteLine("  [warm-up iteration]");
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
            TaskStats? stats = null;
            if (durations.Count > 0)
            {
                var arr = durations.ToArray();
                stats = new TaskStats(
                    StatsCalculator.Mean(arr),
                    StatsCalculator.Median(arr),
                    StatsCalculator.StdDev(arr),
                    StatsCalculator.Min(arr),
                    StatsCalculator.Max(arr));
            }

            results.Add(new BenchmarkResult(task.Name, durations, stats, taskSuccess[task.Name]));
        }

        return new SuiteResult(suite.Name, results);
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
