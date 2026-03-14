using System.Diagnostics;

namespace DevMachineBenchmark.Benchmarks;

public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Elapsed);

public static class ProcessRunner
{
    // On Windows, script-based CLI tools (npm, pnpm, etc.) are .cmd files and cannot
    // be launched directly with UseShellExecute=false. Wrap them via cmd.exe instead.
    private static (string resolvedFileName, string resolvedArguments) ResolveCommand(string fileName, string arguments)
    {
        if (!OperatingSystem.IsWindows())
            return (fileName, arguments);

        // If the file already has an extension or is an absolute path, use as-is
        if (Path.HasExtension(fileName) || Path.IsPathRooted(fileName))
            return (fileName, arguments);

        // Check if a .cmd wrapper exists on PATH; if so, route through cmd.exe
        var cmdPath = FindOnPath(fileName + ".cmd");
        if (cmdPath is not null)
        {
            var quotedArgs = string.IsNullOrEmpty(arguments)
                ? $"/c \"{cmdPath}\""
                : $"/c \"{cmdPath}\" {arguments}";
            return ("cmd.exe", quotedArgs);
        }

        return (fileName, arguments);
    }

    private static string? FindOnPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir, fileName);
            if (File.Exists(full))
                return full;
        }
        return null;
    }

    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken ct = default,
        Dictionary<string, string>? environmentVariables = null)
    {
        var (resolvedFileName, resolvedArguments) = ResolveCommand(fileName, arguments);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = resolvedFileName,
            Arguments = resolvedArguments,
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
