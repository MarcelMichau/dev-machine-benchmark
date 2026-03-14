namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class GitCloneTask(string repoUrl, string targetDir, bool shallow = true) : IBenchmarkTask
{
    public string Name => "git clone";

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var depthArg = shallow ? "--depth 1 " : "";
        var result = await ProcessRunner.RunAsync(
            "git", $"clone {depthArg}{repoUrl} {targetDir}",
            workingDirectory, ct);

        var error = result.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError)
            : null;
        return new TaskResult(result.Elapsed, result.ExitCode == 0, error);
    }
}
