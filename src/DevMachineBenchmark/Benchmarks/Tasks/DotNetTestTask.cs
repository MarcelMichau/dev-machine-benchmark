namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class DotNetTestTask(string projectPath, string label = "unit") : IBenchmarkTask
{
    public string Name => $"dotnet test ({label}): {Path.GetFileName(projectPath.TrimEnd('/', '\\'))}";

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var fullPath = Path.GetFullPath(Path.Combine(workingDirectory, projectPath));

        if (!Directory.Exists(fullPath))
            return new TaskResult(TimeSpan.Zero, false, $"Project directory not found: {fullPath}");
        var result = await ProcessRunner.RunAsync(
            "dotnet", "test --no-build",
            fullPath, ct);

        var error = result.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError)
            : null;
        return new TaskResult(result.Elapsed, result.ExitCode == 0, error);
    }
}
