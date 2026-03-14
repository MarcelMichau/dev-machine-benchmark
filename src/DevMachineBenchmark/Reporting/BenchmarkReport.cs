using DevMachineBenchmark.Benchmarks;
using DevMachineBenchmark.SystemInfo;

namespace DevMachineBenchmark.Reporting;

public sealed record BenchmarkReport(
    MachineInfo MachineInfo,
    DateTime Timestamp,
    int Iterations,
    List<SuiteResult> Suites,
    string? Note = null,
    int WarmupIterations = 1);
