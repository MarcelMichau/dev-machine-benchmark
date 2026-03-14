namespace DevMachineBenchmark.Benchmarks.Tasks;

public sealed class GitCommitTask(string? repoSubDir = null) : IBenchmarkTask
{
    public string Name => "git add + commit";
    public TaskCategory Category => TaskCategory.IO;

    public async Task<TaskResult> ExecuteAsync(string workingDirectory, CancellationToken ct)
    {
        var repoDir = repoSubDir is not null
            ? Path.Combine(workingDirectory, repoSubDir)
            : workingDirectory;

        // Configure git identity for the commit
        await ProcessRunner.RunAsync("git", "config user.email benchmark@test.local", repoDir, ct);
        await ProcessRunner.RunAsync("git", "config user.name Benchmark", repoDir, ct);

        // Fixed dates for reproducibility
        var env = new Dictionary<string, string>
        {
            ["GIT_AUTHOR_DATE"] = "2025-01-01T00:00:00+00:00",
            ["GIT_COMMITTER_DATE"] = "2025-01-01T00:00:00+00:00",
        };

        // Stage all files — this is where DLP/endpoint protection overhead is most visible
        var addResult = await ProcessRunner.RunAsync("git", "add -A", repoDir, ct, env);
        if (addResult.ExitCode != 0)
            return new TaskResult(addResult.Elapsed, false, addResult.StandardError);

        // Commit with --no-verify to skip hooks and -c commit.gpgsign=false to
        // bypass GPG/1Password signing (measure raw git perf, not signing overhead)
        // --allow-empty in case nothing changed
        var commitResult = await ProcessRunner.RunAsync(
            "git", "-c commit.gpgsign=false commit --no-verify --allow-empty -m benchmark",
            repoDir, ct, env);

        var totalElapsed = addResult.Elapsed + commitResult.Elapsed;
        return new TaskResult(totalElapsed, commitResult.ExitCode == 0,
            commitResult.ExitCode != 0 ? commitResult.StandardError : null);
    }
}
