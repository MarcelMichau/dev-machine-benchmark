namespace DevMachineBenchmark.Benchmarks;

public sealed record BenchmarkSuite(
    string Name,
    string? RepoUrl,
    List<IBenchmarkTask> Tasks);

public sealed record SuiteResult(
    string SuiteName,
    List<BenchmarkResult> Results,
    TimeSpan TotalElapsed = default);
