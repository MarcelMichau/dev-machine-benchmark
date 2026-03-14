namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class DockerPullTask(string image) : IBenchmarkTask
{
    public string Name => $"docker pull {image}";

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        // Remove image first to ensure we measure a fresh pull
        await ProcessRunner.RunAsync("docker", $"rmi {image}", workingDirectory, ct);

        var result = await ProcessRunner.RunAsync("docker", $"pull {image}", workingDirectory, ct);

        return new TaskResult(result.Elapsed, result.ExitCode == 0,
            result.ExitCode != 0 ? result.StandardError : null);
    }
}
