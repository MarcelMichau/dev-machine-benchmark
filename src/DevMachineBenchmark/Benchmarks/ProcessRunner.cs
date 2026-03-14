using System.Diagnostics;

namespace DevMachineBenchmark.Benchmarks;

public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Elapsed);

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken ct = default,
        Dictionary<string, string>? environmentVariables = null)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Always set CI=true to prevent interactive modes (e.g. Vitest watch)
        process.StartInfo.EnvironmentVariables["CI"] = "true";

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
                process.StartInfo.EnvironmentVariables[key] = value;
        }

        var stopwatch = Stopwatch.StartNew();

        process.Start();

        // Read stdout and stderr asynchronously to avoid deadlocks
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(ct);

        stopwatch.Stop();

        return new ProcessResult(
            process.ExitCode,
            stdoutTask.Result,
            stderrTask.Result,
            stopwatch.Elapsed);
    }
}
