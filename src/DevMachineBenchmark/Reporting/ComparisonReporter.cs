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
        Console.WriteLine();

        // Build lookup from task name to median for each report
        var mediansA = BuildMedianLookup(reportA);
        var mediansB = BuildMedianLookup(reportB);

        var allKeys = mediansA.Keys.Union(mediansB.Keys).OrderBy(k => k).ToList();

        Console.WriteLine(string.Format("  {0,-50} {1,12} {2,12} {3,10} {4,10}",
            "Task", nameA, nameB, "Diff %", "Winner"));
        Console.WriteLine("  " + new string('-', 96));

        foreach (var key in allKeys)
        {
            var hasA = mediansA.TryGetValue(key, out var medA);
            var hasB = mediansB.TryGetValue(key, out var medB);

            var colA = hasA ? FormatMs(medA) : "N/A";
            var colB = hasB ? FormatMs(medB) : "N/A";
            var diff = "";
            var winner = "";

            if (hasA && hasB && medA > 0 && medB > 0)
            {
                var pct = (medB - medA) / medA * 100;
                diff = $"{pct:+0.0;-0.0}%";

                if (Math.Abs(pct) > 5)
                {
                    winner = pct > 0 ? nameA : nameB;
                    Console.ForegroundColor = pct > 0 ? ConsoleColor.Green : ConsoleColor.Red;
                }
            }

            Console.WriteLine(string.Format("  {0,-50} {1,12} {2,12} {3,10} {4,10}",
                key, colA, colB, diff, winner));
            Console.ResetColor();
        }

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

    private static string FormatMs(double ms) =>
        ms >= 1000 ? $"{ms / 1000:F1}s" : $"{ms:F0}ms";
}
