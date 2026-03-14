namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class DotNetBuildTask(string solution) : IBenchmarkTask
{
    public string Name => $"dotnet build {solution.Replace('\\', '/')}";
    public TaskCategory Category => TaskCategory.Cpu;

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var result = await ProcessRunner.RunAsync(
            "dotnet", $"build {solution} --no-restore",
            workingDirectory, ct);

        var error = result.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError)
            : null;
        return new TaskResult(result.Elapsed, result.ExitCode == 0, error);
    }
}
