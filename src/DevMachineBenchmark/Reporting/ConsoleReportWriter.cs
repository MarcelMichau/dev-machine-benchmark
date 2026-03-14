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
        Console.WriteLine($"  Iterations: {report.Iterations}");
        Console.WriteLine($"  Timestamp:  {report.Timestamp:yyyy-MM-dd HH:mm:ss}");

        foreach (var suite in report.Suites)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  --- {suite.SuiteName} ---");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine(string.Format("  {0,-50} {1,10} {2,10} {3,10} {4,10}",
                "Task", "Median", "StdDev", "Min", "Max"));
            Console.WriteLine("  " + new string('-', 92));

            foreach (var result in suite.Results)
            {
                if (!result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("  {0,-50} {1,10}", result.TaskName, "FAILED"));
                    Console.ResetColor();
                    continue;
                }

                if (result.Stats is null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(string.Format("  {0,-50} {1,10}", result.TaskName, "SKIPPED"));
                    Console.ResetColor();
                    continue;
                }

                var s = result.Stats;
                Console.WriteLine(string.Format("  {0,-50} {1,10} {2,10} {3,10} {4,10}",
                    result.TaskName,
                    FormatMs(s.MedianMs),
                    FormatMs(s.StdDevMs),
                    FormatMs(s.MinMs),
                    FormatMs(s.MaxMs)));
            }
        }

        if (report.Note is not null)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Note: {report.Note}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    private static string FormatMs(double ms) =>
        ms >= 1000 ? $"{ms / 1000:F1}s" : $"{ms:F0}ms";
}
