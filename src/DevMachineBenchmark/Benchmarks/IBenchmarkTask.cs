namespace DevMachineBenchmark.Benchmarks;

public sealed record TaskResult(TimeSpan Elapsed, bool Success, string? ErrorMessage = null);

public interface IBenchmarkTask
{
    string Name { get; }
    Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct);
}
