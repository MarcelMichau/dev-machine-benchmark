namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class PnpmBuildTask(string subDirectory) : IBenchmarkTask
{
    public string Name => $"pnpm run build ({subDirectory})";
    public TaskCategory Category => TaskCategory.Cpu;

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var fullPath = Path.GetFullPath(Path.Combine(workingDirectory, subDirectory));
        var result = await ProcessRunner.RunAsync("pnpm", "run build", fullPath, ct);

        var error = result.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError)
            : null;
        return new TaskResult(result.Elapsed, result.ExitCode == 0, error);
    }
}
