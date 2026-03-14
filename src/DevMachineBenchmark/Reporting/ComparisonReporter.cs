using DevMachineBenchmark.Benchmarks;

namespace DevMachineBenchmark.Reporting;

public static class ComparisonReporter
{
    public static void Compare(string fileA, string fileB)
    {
        var reportA = JsonReportWriter.Read(fileA);
        var reportB = JsonReportWriter.Read(fileB);

        if (reportA is null || reportB is null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Failed to deserialize one or both report files.");
            Console.ResetColor();
            return;
        }

        var nameA = reportA.MachineInfo.Hostname;
        var nameB = reportB.MachineInfo.Hostname;

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                          BENCHMARK COMPARISON                                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine();
        Console.WriteLine($"  Machine A: {nameA} ({reportA.MachineInfo.Os})");
        Console.WriteLine($"  Machine B: {nameB} ({reportB.MachineInfo.Os})");
        Console.WriteLine($"  Iterations: A={reportA.Iterations}, B={reportB.Iterations}");
        Console.WriteLine();

        // Warn about small sample sizes
        var minIterations = Math.Min(reportA.Iterations, reportB.Iterations);
        if (minIterations < 5)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Warning: One or both reports have fewer than 5 iterations (A={reportA.Iterations}, B={reportB.Iterations}).");
            Console.WriteLine("  Differences may not be statistically significant. Use --iterations 5+ for reliable comparisons.");
            Console.ResetColor();
            Console.WriteLine();
        }

        // Build lookups
        var mediansA = BuildMedianLookup(reportA);
        var mediansB = BuildMedianLookup(reportB);
        var cvsA = BuildCvLookup(reportA);
        var cvsB = BuildCvLookup(reportB);
        var categoriesA = BuildCategoryLookup(reportA);
        var categoriesB = BuildCategoryLookup(reportB);

        var allKeys = mediansA.Keys.Union(mediansB.Keys).OrderBy(k => k).ToList();

        Console.WriteLine(string.Format("  {0,-50} {1,6} {2,12} {3,12} {4,10} {5,10}",
            "Task", "Type", nameA, nameB, "Diff %", "Winner"));
        Console.WriteLine("  " + new string('-', 104));

        foreach (var key in allKeys)
        {
            var hasA = mediansA.TryGetValue(key, out var medA);
            var hasB = mediansB.TryGetValue(key, out var medB);
            cvsA.TryGetValue(key, out var cvA);
            cvsB.TryGetValue(key, out var cvB);
            categoriesA.TryGetValue(key, out var catA);
            categoriesB.TryGetValue(key, out var catB);
            var category = catA ?? catB;

            var colA = hasA ? FormatMs(medA) : "N/A";
            var colB = hasB ? FormatMs(medB) : "N/A";
            var diff = "";
            var winner = "";

            if (hasA && hasB && medA > 0 && medB > 0)
            {
                var pct = (medB - medA) / medA * 100;
                diff = $"{pct:+0.0;-0.0}%";

                // Only declare a winner if the difference exceeds noise
                var noisy = (cvA is > 30) || (cvB is > 30);
                var threshold = noisy ? 20.0 : 5.0;

                if (Math.Abs(pct) > threshold)
                {
                    winner = pct > 0 ? nameA : nameB;
                    Console.ForegroundColor = pct > 0 ? ConsoleColor.Green : ConsoleColor.Red;
                }

                if (noisy)
                    diff += "*";
            }

            Console.WriteLine(string.Format("  {0,-50} {1,6} {2,12} {3,12} {4,10} {5,10}",
                key, CategoryTag(category), colA, colB, diff, winner));
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  * = high variance (CV > 30%); difference may not be meaningful");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static Dictionary<string, double> BuildMedianLookup(BenchmarkReport report)
    {
        var lookup = new Dictionary<string, double>();
        foreach (var suite in report.Suites)
        {
            foreach (var result in suite.Results)
            {
                if (result.Stats is not null)
                    lookup[result.TaskName] = result.Stats.MedianMs;
            }
        }
        return lookup;
    }

    private static Dictionary<string, double?> BuildCvLookup(BenchmarkReport report)
    {
        var lookup = new Dictionary<string, double?>();
        foreach (var suite in report.Suites)
        {
            foreach (var result in suite.Results)
            {
                if (result.Stats is not null)
                    lookup[result.TaskName] = result.Stats.CvPercent;
            }
        }
        return lookup;
    }

    private static Dictionary<string, TaskCategory?> BuildCategoryLookup(BenchmarkReport report)
    {
        var lookup = new Dictionary<string, TaskCategory?>();
        foreach (var suite in report.Suites)
        {
            foreach (var result in suite.Results)
            {
                lookup[result.TaskName] = result.Category;
            }
        }
        return lookup;
    }

    private static string CategoryTag(TaskCategory? category) =>
        category switch
        {
            TaskCategory.Network => "NET",
            TaskCategory.Cpu => "CPU",
            TaskCategory.IO => "I/O",
            TaskCategory.Mixed => "MIX",
            _ => "—",
        };

    private static string FormatMs(double ms) =>
        ms >= 1000 ? $"{ms / 1000:F1}s" : $"{ms:F0}ms";
}
