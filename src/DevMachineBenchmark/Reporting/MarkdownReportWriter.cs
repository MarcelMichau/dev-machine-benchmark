using System.Globalization;
using System.Text;
using DevMachineBenchmark.Benchmarks;

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
        sb.AppendLine($"_{report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC · {report.Iterations} iterations (+{report.WarmupIterations} warm-up discarded)_");
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
        if (report.MachineInfo.CpuUsagePercent is not null)
            sb.AppendLine($"| **CPU Load at Start** | {report.MachineInfo.CpuUsagePercent.Value.ToString("F1", CultureInfo.InvariantCulture)}% |");
        if (report.MachineInfo.MemoryUsagePercent is not null)
            sb.AppendLine($"| **Memory Used at Start** | {report.MachineInfo.MemoryUsagePercent.Value.ToString("F1", CultureInfo.InvariantCulture)}% |");
        sb.AppendLine();

        // Per-suite results
        sb.AppendLine("## Results");
        sb.AppendLine();

        foreach (var suite in report.Suites)
        {
            sb.AppendLine($"### {suite.SuiteName}");
            sb.AppendLine();
            sb.AppendLine("| Task | Type | Median | StdDev | CV% | Min | Max | 95% CI |");
            sb.AppendLine("|------|:----:|-------:|-------:|----:|----:|----:|-------:|");

            foreach (var result in suite.Results)
            {
                if (!result.Success || result.Stats is null)
                {
                    sb.AppendLine($"| {result.TaskName} | {CategoryTag(result.Category)} | FAILED | — | — | — | — | — |");
                    continue;
                }

                var s = result.Stats;
                var stddev = result.DurationsMs.Count > 1 ? FormatMs(s.StdDevMs) : "—";
                var cv = s.CvPercent is not null ? $"{s.CvPercent.Value.ToString("F1", CultureInfo.InvariantCulture)}%" : "—";
                var ci = s.CiLowMs is not null && s.CiHighMs is not null
                    ? $"[{FormatMs(s.CiLowMs.Value)}, {FormatMs(s.CiHighMs.Value)}]"
                    : "—";
                var outlierFlag = s.HasOutliers ? " ⚠️" : "";

                sb.AppendLine($"| {result.TaskName}{outlierFlag} | {CategoryTag(result.Category)} | {FormatMs(s.MedianMs)} | {stddev} | {cv} | {FormatMs(s.MinMs)} | {FormatMs(s.MaxMs)} | {ci} |");
            }

            sb.AppendLine();
        }

        // Legend
        sb.AppendLine("### Legend");
        sb.AppendLine();
        sb.AppendLine("| Type | Description |");
        sb.AppendLine("|:----:|-------------|");
        sb.AppendLine("| NET | Network-dependent (clone, pull, install) |");
        sb.AppendLine("| CPU | CPU-bound (build, compile) |");
        sb.AppendLine("| I/O | Disk I/O-bound (git commit) |");
        sb.AppendLine("| MIX | Mixed workload (tests) |");
        sb.AppendLine();

        // Notes
        if (report.Note is not null)
        {
            sb.AppendLine("## Notes");
            sb.AppendLine();
            sb.AppendLine($"> {report.Note}");
            sb.AppendLine();
        }

        if (report.Iterations < 5)
        {
            sb.AppendLine($"> ⚠️ Only {report.Iterations} iteration(s) measured. Results may not be statistically reliable. Consider using `--iterations 5` or higher.");
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(filePath, sb.ToString());
        return filePath;
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
        ms >= 1000
            ? $"{(ms / 1000).ToString("F2", CultureInfo.InvariantCulture)}s"
            : $"{ms.ToString("F0", CultureInfo.InvariantCulture)}ms";
}
