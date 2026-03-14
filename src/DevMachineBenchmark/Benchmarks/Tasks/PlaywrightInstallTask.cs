namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class PlaywrightInstallTask(string subDirectory, bool usePnpm = false) : IBenchmarkTask
{
    public string Name => $"playwright install ({subDirectory})";
    public TaskCategory Category => TaskCategory.Network;

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var fullPath = Path.GetFullPath(Path.Combine(workingDirectory, subDirectory));

        var (cmd, args) = usePnpm
            ? ("pnpm", "exec playwright install")
            : ("npx", "playwright install");

        var result = await ProcessRunner.RunAsync(cmd, args, fullPath, ct);

        var error = result.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError)
            : null;
        return new TaskResult(result.Elapsed, result.ExitCode == 0, error);
    }
}
