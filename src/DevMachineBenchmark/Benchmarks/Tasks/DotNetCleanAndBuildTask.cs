namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class DotNetCleanAndBuildTask(string solution) : IBenchmarkTask
{
    public string Name => $"dotnet clean + build {solution.Replace('\\', '/')}";
    public TaskCategory Category => TaskCategory.Cpu;

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var cleanResult = await ProcessRunner.RunAsync(
            "dotnet", $"clean {solution}",
            workingDirectory, ct);

        if (cleanResult.ExitCode != 0)
        {
            var error = string.IsNullOrWhiteSpace(cleanResult.StandardError)
                ? cleanResult.StandardOutput
                : cleanResult.StandardError;
            return new TaskResult(cleanResult.Elapsed, false, error);
        }

        var buildResult = await ProcessRunner.RunAsync(
            "dotnet", $"build {solution} --no-restore",
            workingDirectory, ct);

        var buildError = buildResult.ExitCode != 0
            ? (string.IsNullOrWhiteSpace(buildResult.StandardError) ? buildResult.StandardOutput : buildResult.StandardError)
            : null;

        return new TaskResult(cleanResult.Elapsed + buildResult.Elapsed, buildResult.ExitCode == 0, buildError);
    }
}
