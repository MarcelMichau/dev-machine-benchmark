using DevMachineBenchmark.Benchmarks;

namespace DevMachineBenchmark.Reporting;

public static class ConsoleReportWriter
{
    public static void Write(BenchmarkReport report)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                     BENCHMARK RESULTS                           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine();
        Console.WriteLine($"  Machine:    {report.MachineInfo.Hostname}");
        Console.WriteLine($"  OS:         {report.MachineInfo.Os}");
        Console.WriteLine($"  CPU:        {report.MachineInfo.CpuModel} ({report.MachineInfo.CpuCores} cores)");
        Console.WriteLine($"  RAM:        {report.MachineInfo.RamGB} GB");
        Console.WriteLine($"  Disk:       {report.MachineInfo.DiskType}");
        Console.WriteLine($"  .NET SDK:   {report.MachineInfo.DotnetSdk}");
        Console.WriteLine($"  Node:       {report.MachineInfo.NodeVersion ?? "N/A"}");
        Console.WriteLine($"  Docker:     {report.MachineInfo.DockerVersion ?? "N/A"}");
        Console.WriteLine($"  Iterations: {report.Iterations} (+ {report.WarmupIterations} warm-up)");
        Console.WriteLine($"  Timestamp:  {report.Timestamp:yyyy-MM-dd HH:mm:ss}");

        if (report.MachineInfo.CpuUsagePercent is not null)
            Console.WriteLine($"  CPU Load:   {report.MachineInfo.CpuUsagePercent:F1}% at start");
        if (report.MachineInfo.MemoryUsagePercent is not null)
            Console.WriteLine($"  Memory:     {report.MachineInfo.MemoryUsagePercent:F1}% used at start");

        foreach (var suite in report.Suites)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  --- {suite.SuiteName} ---");
            Console.ResetColor();
            Console.WriteLine();

            var taskColWidth = Math.Max(4, suite.Results.Count > 0 ? suite.Results.Max(r => r.TaskName.Length) : 4);
            var separatorWidth = taskColWidth + 6 + 10 + 10 + 10 + 10 + 7 + 5;
            var fmt = $"  {{0,-{taskColWidth}}} {{1,6}} {{2,10}} {{3,10}} {{4,10}} {{5,10}} {{6,7}}";
            var fmtShort = $"  {{0,-{taskColWidth}}} {{1,6}} {{2,10}}";

            Console.WriteLine(fmt, "Task", "Type", "Median", "StdDev", "Min", "Max", "CV%");
            Console.WriteLine("  " + new string('-', separatorWidth));

            foreach (var result in suite.Results)
            {
                if (!result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(fmtShort, result.TaskName, CategoryTag(result.Category), "FAILED");
                    Console.ResetColor();
                    continue;
                }

                if (result.Stats is null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(fmtShort, result.TaskName, CategoryTag(result.Category), "SKIPPED");
                    Console.ResetColor();
                    continue;
                }

                var s = result.Stats;
                var stddev = result.DurationsMs.Count > 1 ? FormatMs(s.StdDevMs) : "N/A";
                var cv = s.CvPercent is not null ? $"{s.CvPercent:F1}%" : "N/A";

                // Flag outliers
                var outlierFlag = s.HasOutliers ? " (!)" : "";

                // Color high CV yellow
                if (s.CvPercent is > 30)
                    Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine(fmt + "{7}", result.TaskName, CategoryTag(result.Category), FormatMs(s.MedianMs), stddev, FormatMs(s.MinMs), FormatMs(s.MaxMs), cv, outlierFlag);

                Console.ResetColor();

                // Show confidence interval when available
                if (s.CiLowMs is not null && s.CiHighMs is not null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"    95% CI: [{FormatMs(s.CiLowMs.Value)}, {FormatMs(s.CiHighMs.Value)}]" +
                        (s.TrimmedMeanMs is not null ? $"  Trimmed Mean: {FormatMs(s.TrimmedMeanMs.Value)}" : "") +
                        (s.IqrMs is not null ? $"  IQR: {FormatMs(s.IqrMs.Value)}" : ""));
                    Console.ResetColor();
                }
            }
        }

        if (report.Note is not null)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Note: {report.Note}");
            Console.ResetColor();
        }

        // Warn if iteration count is low
        if (report.Iterations < 5)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Warning: Only {report.Iterations} iteration(s) measured. Consider --iterations 5 or higher for reliable statistics.");
            Console.ResetColor();
        }

        Console.WriteLine();
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
