namespace DevMachineBenchmark.Benchmarks;

public enum TaskCategory
{
    Network,
    Cpu,
    IO,
    Mixed,
}

public sealed record TaskResult(TimeSpan Elapsed, bool Success, string? ErrorMessage = null);

public interface IBenchmarkTask
{
    string Name { get; }
    TaskCategory Category { get; }
    Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct);
}
