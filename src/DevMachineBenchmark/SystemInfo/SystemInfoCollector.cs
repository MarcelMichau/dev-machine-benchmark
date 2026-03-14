using System.Runtime.InteropServices;
using DevMachineBenchmark.Benchmarks;

namespace DevMachineBenchmark.SystemInfo;

public static class SystemInfoCollector
{
    public static async Task<MachineInfo> CollectAsync()
    {
        var hostname = Environment.MachineName;
        var os = RuntimeInformation.OSDescription;
        var cpuCores = Environment.ProcessorCount;
        var ramBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var ramGB = Math.Round(ramBytes / (1024.0 * 1024 * 1024), 1);

        var cpuModel = await GetCpuModelAsync();
        var diskType = await GetDiskTypeAsync();
        var dotnetSdk = await GetToolVersionAsync("dotnet", "--version") ?? "unknown";
        var nodeVersion = await GetToolVersionAsync("node", "--version");
        var dockerVersion = await GetDockerVersionAsync();

        return new MachineInfo(
            hostname, os, cpuModel, cpuCores, ramGB,
            diskType, dotnetSdk, nodeVersion, dockerVersion);
    }

    private static async Task<string> GetCpuModelAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // wmic is deprecated; use PowerShell instead
                var result = await ProcessRunner.RunAsync(
                    "powershell",
                    "-NoProfile -Command \"(Get-CimInstance Win32_Processor | Select-Object -First 1).Name\"",
                    ".");
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
                    return result.StandardOutput.Trim();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var result = await ProcessRunner.RunAsync("sysctl", "-n machdep.cpu.brand_string", ".");
                if (result.ExitCode == 0)
                    return result.StandardOutput.Trim();
            }
            else
            {
                var result = await ProcessRunner.RunAsync(
                    "bash", "-c \"grep -m1 'model name' /proc/cpuinfo | cut -d: -f2\"", ".");
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
                    return result.StandardOutput.Trim();
            }
        }
        catch { }

        return "unknown";
    }

    private static async Task<string> GetDiskTypeAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = await ProcessRunner.RunAsync(
                    "powershell",
                    "-NoProfile -Command \"(Get-PhysicalDisk | Select-Object -First 1).MediaType\"",
                    ".");
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
                    return result.StandardOutput.Trim();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var result = await ProcessRunner.RunAsync("diskutil", "info /", ".");
                if (result.ExitCode == 0)
                {
                    var line = result.StandardOutput
                        .Split('\n')
                        .FirstOrDefault(l => l.Contains("Solid State", StringComparison.OrdinalIgnoreCase));
                    if (line is not null)
                        return line.Contains("Yes", StringComparison.OrdinalIgnoreCase) ? "SSD" : "HDD";
                }
            }
        }
        catch { }

        return "unknown";
    }

    private static async Task<string?> GetToolVersionAsync(string tool, string args)
    {
        try
        {
            var result = await ProcessRunner.RunAsync(tool, args, ".");
            return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> GetDockerVersionAsync()
    {
        var version = await GetToolVersionAsync("docker", "--version");
        if (version is not null) return version;
        return await GetToolVersionAsync("podman", "--version");
    }
}
