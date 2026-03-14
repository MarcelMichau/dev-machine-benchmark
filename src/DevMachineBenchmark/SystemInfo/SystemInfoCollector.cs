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
        var cpuUsage = await GetCpuUsageAsync();
        var memoryUsage = await GetMemoryUsagePercentAsync();

        return new MachineInfo(
            hostname, os, cpuModel, cpuCores, ramGB,
            diskType, dotnetSdk, nodeVersion, dockerVersion,
            cpuUsage, memoryUsage);
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

    private static async Task<double?> GetCpuUsageAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = await ProcessRunner.RunAsync(
                    "powershell",
                    "-NoProfile -Command \"(Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average\"",
                    ".");
                if (result.ExitCode == 0 && double.TryParse(result.StandardOutput.Trim(), out var cpu))
                    return Math.Round(cpu, 1);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var result = await ProcessRunner.RunAsync(
                    "bash", "-c \"top -l 1 | grep 'CPU usage' | awk '{print $3}' | tr -d '%'\"", ".");
                if (result.ExitCode == 0 && double.TryParse(result.StandardOutput.Trim(), out var cpu))
                    return Math.Round(cpu, 1);
            }
            else
            {
                var result = await ProcessRunner.RunAsync(
                    "bash", "-c \"grep 'cpu ' /proc/stat | awk '{usage=($2+$4)*100/($2+$4+$5)} END {print usage}'\"", ".");
                if (result.ExitCode == 0 && double.TryParse(result.StandardOutput.Trim(), out var cpu))
                    return Math.Round(cpu, 1);
            }
        }
        catch { }

        return null;
    }

    private static async Task<double?> GetMemoryUsagePercentAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = await ProcessRunner.RunAsync(
                    "powershell",
                    "-NoProfile -Command \"$os = Get-CimInstance Win32_OperatingSystem; [math]::Round(($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / $os.TotalVisibleMemorySize * 100, 1)\"",
                    ".");
                if (result.ExitCode == 0 && double.TryParse(result.StandardOutput.Trim(), out var mem))
                    return mem;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var result = await ProcessRunner.RunAsync(
                    "bash", "-c \"vm_stat | awk '/Pages active/ {active=$3} /Pages wired/ {wired=$4} /Pages free/ {free=$3} END {total=active+wired+free; print (active+wired)/total*100}'\"", ".");
                if (result.ExitCode == 0 && double.TryParse(result.StandardOutput.Trim(), out var mem))
                    return Math.Round(mem, 1);
            }
            else
            {
                var result = await ProcessRunner.RunAsync(
                    "bash", "-c \"free | awk '/Mem:/ {print $3/$2 * 100}'\"", ".");
                if (result.ExitCode == 0 && double.TryParse(result.StandardOutput.Trim(), out var mem))
                    return Math.Round(mem, 1);
            }
        }
        catch { }

        return null;
    }
}
