namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class DotNetRestoreTask(string solution) : IBenchmarkTask
{
    public string Name => $"dotnet restore {solution.Replace('\\', '/')}";

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var result = await ProcessRunner.RunAsync(
            "dotnet", $"restore {solution}",
            workingDirectory, ct);

        var error = result.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError)
            : null;
        return new TaskResult(result.Elapsed, result.ExitCode == 0, error);
    }
}
