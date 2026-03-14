using System.Globalization;
using System.Text;

namespace DevMachineBenchmark.Reporting;

public static class MarkdownReportWriter
{
    public static async Task<string> WriteAsync(BenchmarkReport report, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var hostname = report.MachineInfo.Hostname;
        var timestamp = report.Timestamp.ToString("yyyyMMdd-HHmmss");
        var fileName = $"benchmark-{hostname}-{timestamp}.md";
        var filePath = Path.Combine(outputDir, fileName);

        var sb = new StringBuilder();

        sb.AppendLine($"# Benchmark Results — {hostname}");
        sb.AppendLine();
        sb.AppendLine($"_{report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC · {report.Iterations} iterations (+1 warm-up discarded)_");
        sb.AppendLine();

        // Machine info
        sb.AppendLine("## Machine");
        sb.AppendLine();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| **Hostname** | {report.MachineInfo.Hostname} |");
        sb.AppendLine($"| **OS** | {report.MachineInfo.Os} |");
        sb.AppendLine($"| **CPU** | {report.MachineInfo.CpuModel} ({report.MachineInfo.CpuCores} cores) |");
        sb.AppendLine($"| **RAM** | {report.MachineInfo.RamGB.ToString("F1", CultureInfo.InvariantCulture)} GB |");
        sb.AppendLine($"| **Disk** | {report.MachineInfo.DiskType} |");
        sb.AppendLine($"| **.NET SDK** | {report.MachineInfo.DotnetSdk} |");
        sb.AppendLine($"| **Node** | {report.MachineInfo.NodeVersion ?? "N/A"} |");
        sb.AppendLine($"| **Docker** | {report.MachineInfo.DockerVersion ?? "N/A"} |");
        sb.AppendLine();

        // Per-suite results
        sb.AppendLine("## Results");
        sb.AppendLine();

        foreach (var suite in report.Suites)
        {
            sb.AppendLine($"### {suite.SuiteName}");
            sb.AppendLine();
            sb.AppendLine("| Task | Median | StdDev | Min | Max |");
            sb.AppendLine("|------|-------:|-------:|----:|----:|");

            foreach (var result in suite.Results)
            {
                if (!result.Success || result.Stats is null)
                {
                    sb.AppendLine($"| {result.TaskName} | FAILED | — | — | — |");
                    continue;
                }

                var s = result.Stats;
                var stddev = result.DurationsMs.Count > 1 ? FormatMs(s.StdDevMs) : "—";
                sb.AppendLine($"| {result.TaskName} | {FormatMs(s.MedianMs)} | {stddev} | {FormatMs(s.MinMs)} | {FormatMs(s.MaxMs)} |");
            }

            sb.AppendLine();
        }

        // Notes
        if (report.Note is not null)
        {
            sb.AppendLine("## Notes");
            sb.AppendLine();
            sb.AppendLine($"> {report.Note}");
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
        return filePath;
    }

    private static string FormatMs(double ms) =>
        ms >= 1000
            ? $"{(ms / 1000).ToString("F2", CultureInfo.InvariantCulture)}s"
            : $"{ms.ToString("F0", CultureInfo.InvariantCulture)}ms";
}
