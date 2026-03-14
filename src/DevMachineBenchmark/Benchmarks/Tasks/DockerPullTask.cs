namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class DockerPullTask(string image, string runtime = "docker") : IBenchmarkTask
{
    public string Name => $"{runtime} pull {image}";
    public TaskCategory Category => TaskCategory.Network;

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        // Remove image first to ensure we measure a fresh pull
        await ProcessRunner.RunAsync(runtime, $"rmi {image}", workingDirectory, ct);

        var result = await ProcessRunner.RunAsync(runtime, $"pull {image}", workingDirectory, ct);

        return new TaskResult(result.Elapsed, result.ExitCode == 0,
            result.ExitCode != 0 ? result.StandardError : null);
    }
}
