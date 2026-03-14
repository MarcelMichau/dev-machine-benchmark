namespace DevMachineBenchmark.Benchmarks;

public sealed record TaskStats(
    double MeanMs,
    double MedianMs,
    double StdDevMs,
    double MinMs,
    double MaxMs,
    double? CvPercent = null,
    double? CiLowMs = null,
    double? CiHighMs = null,
    double? IqrMs = null,
    double? TrimmedMeanMs = null,
    bool HasOutliers = false,
    int OutlierCount = 0);

public sealed record BenchmarkResult(
    string TaskName,
    List<double> DurationsMs,
    TaskStats? Stats,
    bool Success,
    TaskCategory? Category = null);
