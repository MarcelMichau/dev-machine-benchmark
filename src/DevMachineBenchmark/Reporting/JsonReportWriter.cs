using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevMachineBenchmark.Reporting;

public static class JsonReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<string> WriteAsync(BenchmarkReport report, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var hostname = report.MachineInfo.Hostname;
        var timestamp = report.Timestamp.ToString("yyyyMMdd-HHmmss");
        var fileName = $"benchmark-{hostname}-{timestamp}.json";
        var filePath = Path.Combine(outputDir, fileName);

        var json = JsonSerializer.Serialize(report, Options);
        await File.WriteAllTextAsync(filePath, json);

        return filePath;
    }

    public static BenchmarkReport? Read(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<BenchmarkReport>(json, Options);
    }
}
