namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class PnpmInstallTask(string subDirectory) : IBenchmarkTask
{
    public string Name => $"pnpm install ({subDirectory})";

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var fullPath = Path.GetFullPath(Path.Combine(workingDirectory, subDirectory));
        var result = await ProcessRunner.RunAsync("pnpm", "install", fullPath, ct);

        var error = result.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError)
            : null;
        return new TaskResult(result.Elapsed, result.ExitCode == 0, error);
    }
}
