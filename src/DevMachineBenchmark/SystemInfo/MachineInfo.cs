namespace DevMachineBenchmark.SystemInfo;

public sealed record MachineInfo(
    string Hostname,
    string Os,
    string CpuModel,
    int CpuCores,
    double RamGB,
    string DiskType,
    string DotnetSdk,
    string? NodeVersion,
    string? DockerVersion,
    double? CpuUsagePercent = null,
    double? MemoryUsagePercent = null);
