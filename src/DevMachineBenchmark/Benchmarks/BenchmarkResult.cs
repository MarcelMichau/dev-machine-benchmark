namespace DevMachineBenchmark.Benchmarks;

public sealed record TaskStats(
    double MeanMs,
    double MedianMs,
    double StdDevMs,
    double MinMs,
    double MaxMs);

public sealed record BenchmarkResult(
    string TaskName,
    List<double> DurationsMs,
    TaskStats? Stats,
    bool Success);
